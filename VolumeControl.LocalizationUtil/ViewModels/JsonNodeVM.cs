using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using VolumeControl.LocalizationUtil.Helpers.Collections;
using VolumeControl.Log;

namespace VolumeControl.LocalizationUtil.ViewModels
{
    public abstract class TreeViewNodeVM : INotifyPropertyChanged
    {
        #region Constructor
        protected TreeViewNodeVM(TreeViewNodeVM? parent)
        {
            ParentTreeViewNode = parent;
        }
        #endregion Constructor

        #region Properties
        public TreeViewNodeVM? ParentTreeViewNode { get; }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;

                _isSelected = value;
                NotifyPropertyChanged();
            }
        }
        private bool _isSelected;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value == _isExpanded) return;

                _isExpanded = value;
                NotifyPropertyChanged();
            }
        }
        private bool _isExpanded;
        #endregion Properties

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        #endregion Events
    }
    public abstract class JsonNodeVM : TreeViewNodeVM, INotifyPropertyChanged
    {
        #region Constructor
        /// <summary>
        /// Creates a new <see cref="JsonNodeVM"/> instance.
        /// </summary>
        /// <param name="parent">The <see cref="JsonNodeVM"/> instance that is this node's parent.</param>
        /// <param name="name">The name of this node.</param>
        /// <param name="jToken">The <see cref="Newtonsoft.Json.Linq.JToken"/> instance for this node.</param>
        protected JsonNodeVM(JsonNodeVM parent, string name, JToken jToken) : base(parent)
        {
            _token = jToken;
            Name = name;

            Parent = parent;
            Owner = parent.Owner;
        }
        /// <summary>
        /// Creates a new root <see cref="JsonNodeVM"/> instance.
        /// </summary>
        /// <param name="owner">The <see cref="TranslationConfigVM"/> instance that owns this root node.</param>
        /// <param name="rootObject">The root <see cref="JObject"/> instance.</param>
        protected JsonNodeVM(TranslationConfigVM owner, JObject rootObject) : base(owner)
        {
            IsRootNode = true;

            _token = rootObject;
            Name = null;

            Parent = null;
            Owner = owner;
        }
        #endregion Constructor

        #region Fields
        protected readonly JToken _token;
        public readonly bool IsRootNode;
        public const char NodePathSeparatorChar = '/';
        #endregion Fields

        #region Properties
        public JToken JToken => _token;
        public TranslationConfigVM Owner { get; }
        public JsonNodeVM? Parent { get; }
        public JTokenType Type => _token.Type;
        public string? Name { get; set; }
        #endregion Properties

        #region Methods
        internal JsonNodeVM CreateNode(string name, JToken? jToken)
        {
            if (jToken == null)
                return new JsonValueVM(this, name, string.Empty);

            switch (jToken.Type)
            {
            case JTokenType.Object:
                return new JsonObjectVM(this, name, (JObject)jToken);
            case JTokenType.String:
                return new JsonValueVM(this, name, (JValue)jToken);
            default:
                return new JsonErrorVM(this, name, jToken);
            }
        }
        public static string GetNodePath(JsonNodeVM nodeVM)
        {
            string rootNodeName = nodeVM.Owner.FileName;

            if (nodeVM is JsonObjectVM objectVM && objectVM.IsRootNode)
                return rootNodeName;

            List<string> l = new();
            for (JsonNodeVM node = nodeVM; !node!.IsRootNode; node = node.Parent!)
            {
                l.Add(node.Name!);
            }
            l.Add(rootNodeName);
            l.Reverse();
            var sb = new StringBuilder();
            for (int i = 0, i_max = l.Count; i < i_max; ++i)
            {
                sb.Append(l[i]);
                if (i + 1 < i_max)
                    sb.Append(NodePathSeparatorChar);
            }
            return sb.ToString();
        }
        public string GetNodePath() => GetNodePath(this);
        public JsonNodeVM? FindNode(string[] path, bool returnClosestInsteadOfNull = false)
        {
            if (path.Length == 0)
                return this;

            JsonNodeVM? node = this;
            for (int i = 0, i_max = path.Length, i_last = i_max - 1; i < i_max; ++i)
            {
                // find the next node for the current path
                if (node is JsonObjectVM objectVM)
                { // has subnodes
                    var pathElement = path[i];

                    if (objectVM.FindSubNodeWithName(pathElement) is JsonObjectVM subNode)
                    { // found sub node
                        node = subNode;

                        if (i == i_last)
                            return node;
                    }
                    else if (objectVM.FindValueWithName(pathElement) is JsonNodeVM value)
                    { // found value, there are no more subnodes
                        if (returnClosestInsteadOfNull)
                            return value;
                        return null;
                    }
                }
                else
                {
                    if (returnClosestInsteadOfNull)
                        return node;
                    return null;
                }
            }
            return null;
        }
        #endregion Methods
    }
    public class JsonObjectVM : JsonNodeVM
    {
        #region Constructor
        public JsonObjectVM(JsonNodeVM parent, string name, JObject jObject) : base(parent, name, jObject)
        {
            Initialize();
        }
        private JsonObjectVM(TranslationConfigVM owner, JObject jsonRootObject) : base(owner, jsonRootObject)
        { // creates a root node
            Initialize();
        }
        #endregion Constructor

        #region Properties
        public JObject JObject => (JObject)_token;
        public ObservableImmutableList<JsonObjectVM> SubNodes { get; } = new();
        public ObservableImmutableList<JsonNodeVM> Values { get; } = new();
        #endregion Properties

        #region Methods

        #region AddNode
        public void AddNode(JsonNodeVM node)
        {
            if (node is JsonObjectVM objectVM)
            {
                SubNodes.Add(objectVM);
            }
            else
            {
                Values.Add(node);
            }
        }
        #endregion AddNode

        internal IEnumerable<(string, JToken)> GetTokenPairs()
        {
            List<(string, JToken)> l = new();

            bool hasSubNodes = SubNodes.Count > 0;
            if (hasSubNodes && Values.Count > 0)
            {
                // TODO: LOG ERROR & CONTINUE
            }

            if (hasSubNodes)
            { // subnodes only
                foreach (JsonObjectVM objectVM in SubNodes)
                {
                    JObject jObject = new();
                    foreach (var (key, token) in objectVM.GetTokenPairs()) //< RECURSE
                    {
                        jObject.Add(key, token);
                    }
                    l.Add((objectVM.Name!, jObject));
                }
            }
            else
            { // values only
                foreach (var value in Values)
                {
                    if (value is JsonValueVM valueVM)
                    {
                        l.Add((valueVM.Name!, new JValue(valueVM.Content)));
                    }
                    else
                    {
                        FLog.Warning($"Skipping invalid node \"{value.JToken.Path}\"");
                    }
                }
            }

            return l;
        }
        private void Initialize()
        {
            foreach (var (key, value) in JObject)
            {
                AddNode(CreateNode(key, value));
            }
        }
        public static JsonObjectVM CreateRootNode(TranslationConfigVM owner, JToken jToken)
            => new(owner, (JObject)jToken);
        public JsonNodeVM? FindValueWithName(string name, StringComparison stringComparison = StringComparison.Ordinal)
        {
            foreach (var value in Values)
            {
                if (value.Name!.Equals(name, stringComparison))
                {
                    return value;
                }
            }
            return null;
        }
        public JsonObjectVM? FindSubNodeWithName(string name, StringComparison stringComparison = StringComparison.Ordinal)
        {
            foreach (var subNode in SubNodes)
            {
                if (subNode.Name!.Equals(name, stringComparison))
                {
                    return subNode;
                }
            }
            return null;
        }
        #endregion Methods
    }
    public class JsonValueVM : JsonNodeVM
    {
        #region Constructors
        public JsonValueVM(JsonNodeVM parent, string name, JValue jValue) : base(parent, name, jValue)
        {
            Content = jValue.ToObject<string>();
        }
        public JsonValueVM(JsonNodeVM parent, string name, string content) : base(parent, name, new JValue(content))
        {
            Content = content;
        }
        #endregion Constructors

        #region Properties
        public JValue JValue => (JValue)_token;
        public string? Content
        {
            get => _content;
            set
            {
                _content = value;
                NotifyPropertyChanged();
            }
        }
        private string? _content;
        #endregion Properties
    }
    public class JsonErrorVM : JsonNodeVM
    {
        #region Constructor
        public JsonErrorVM(JsonNodeVM parent, string name, JToken? jToken) : base(parent, name, jToken!)
        {
        }
        #endregion Constructor
    }
    public class TranslationConfigVM : TreeViewNodeVM, INotifyPropertyChanged
    {
        #region Constructor
        /// <summary>
        /// Creates a new <see cref="TranslationConfigVM"/> instance with the specified <paramref name="rootNode"/>.
        /// </summary>
        /// <param name="filePath">The full path of the file that the <paramref name="rootNode"/> was loaded from, or the name of an embedded file if it was loaded from an assembly manifest.</param>
        /// <param name="rootNode">The root node of this JSON file.</param>
        public TranslationConfigVM(string filePath, JObject rootNode) : base(null)
        {
            OriginalFilePath = filePath;
            DirectoryPath = Path.GetDirectoryName(filePath);
            FileName = Path.GetFileName(filePath);
            IsFromActualFile = File.Exists(OriginalFilePath);
            RootNode = JsonObjectVM.CreateRootNode(this, rootNode);

            var extensionStartPos = FileName.IndexOf(".loc.json");
            if (extensionStartPos != 2)
            {
                FLog.Error($"Translation config \"{filePath}\" is invalid; filename \"{FileName}\" is not in the correct format.");
                _fileNameLanguageCode = null!;
            }
            else
            {
                _fileNameLanguageCode = FileName[..2];
            }

            // get the language name & id
            var languageNameNode = RootNode.SubNodes.FirstOrDefault();
            if (languageNameNode == null)
            {
                FLog.Error($"Translation config \"{filePath}\" is invalid; the first key must be \"LanguageName\"!");
            }
            else
            {
                if (languageNameNode.Values.FirstOrDefault() is not JsonValueVM languageNameValue)
                {
                    FLog.Error($"Translation config \"{filePath}\" is invalid; the first value of \"LanguageName\" must set the language ID & name!");
                }
                else
                {
                    LanguageID = languageNameValue.Name!;
                    LanguageName = languageNameValue.Content!;
                }
                RootNode.SubNodes.Remove(languageNameNode);
            }
        }
        #endregion Constructor

        #region Properties
        public string FileName { get; set; }
        public string FileNameLanguageCode
        {
            get => _fileNameLanguageCode;
            set
            {
                if (value.Length > 2)
                    _fileNameLanguageCode = value[..2];
                else if (value.Length <= 2)
                    _fileNameLanguageCode = value;
                NotifyPropertyChanged();
            }
        }
        private string _fileNameLanguageCode;
        public string OriginalFilePath { get; }
        public string? DirectoryPath { get; }
        /// <summary>
        /// Gets whether this instance was loaded from an actual file in the filesystem or just from serialized json data.
        /// </summary>
        public bool IsFromActualFile { get; }
        public JsonObjectVM RootNode { get; }
        public string LanguageID { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        #endregion Properties

        #region Methods

        #region SaveToFile
        private JsonObjectVM PackRootNodeForSerialization()
        {
            return RootNode;
        }
        public void SaveToFile()
        {
            if (!IsFromActualFile) return;

            var rootNodeCopy = RootNode;
            rootNodeCopy.Values.Insert(0, new JsonValueVM(RootNode, LanguageID, LanguageName));

            // TODO
        }
        #endregion SaveToFile

        #region ReloadFromFile
        public void ReloadFromFile()
        {
            // TODO
        }
        #endregion ReloadFromFile

        #region TryCreateInstance
        public static bool TryCreateInstance(string fileName, string serializedJsonData, out TranslationConfigVM translationConfigVM)
        {
            JObject? jObject = null;
            try
            {
                jObject = (JObject?)JsonConvert.DeserializeObject(serializedJsonData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An exception occurred while loading the specified file!\nException Type: \"{ex.GetType()}\"\nException Message: \"{ex.Message}\"", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (jObject != null)
            {
                translationConfigVM = new(fileName, jObject);
                return translationConfigVM != null;
            }

            translationConfigVM = null!;
            return false;
        }
        #endregion TryCreateInstance

        #region FindNode
        public JsonNodeVM? FindNode(string[] path, bool returnClosestInsteadOfNull = false)
            => RootNode.FindNode(path, returnClosestInsteadOfNull);
        #endregion FindNode

        #endregion Methods
    }
}
