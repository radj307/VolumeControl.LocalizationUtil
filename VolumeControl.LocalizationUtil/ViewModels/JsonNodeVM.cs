using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using VolumeControl.LocalizationUtil.Helpers.Collections;
using VolumeControl.LocalizationUtil.JsonConverters;
using VolumeControl.Log;

namespace VolumeControl.LocalizationUtil.ViewModels
{
    [DebuggerDisplay("Name = {Name}")]
    [JsonConverter(typeof(JsonNodeJsonConverter))]
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
        public TranslationConfigVM Owner { get; }
        public JsonNodeVM? Parent { get; }
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
        public abstract JProperty ToJProperty();
        #endregion Methods
    }
    [DebuggerDisplay("Name = {Name}, Values = {Values.Count}, SubNodes = {SubNodes.Count}")]
    public class JsonObjectVM : JsonNodeVM
    {
        #region Constructor
        public JsonObjectVM(JsonNodeVM parent, string name, JObject jObject) : base(parent, name, jObject)
        {
            foreach (var (key, value) in jObject)
            {
                AddNode(CreateNode(key, value));
            }
        }
        private JsonObjectVM(TranslationConfigVM owner, JObject jsonRootObject) : base(owner, jsonRootObject)
        { // creates a root node
            foreach (var (key, value) in jsonRootObject)
            {
                AddNode(CreateNode(key, value));
            }
        }
        private JsonObjectVM(TranslationConfigVM owner) : base(owner, null!) { }
        #endregion Constructor

        #region Properties
        public JObject JObject => (JObject)_token;
        public ObservableImmutableList<JsonObjectVM> SubNodes { get; } = new();
        public ObservableImmutableList<JsonNodeVM> Values { get; } = new();
        #endregion Properties

        #region Methods

        #region CreateRootNode
        public static JsonObjectVM CreateRootNode(TranslationConfigVM owner, JToken jToken)
            => new(owner, (JObject)jToken);
        #endregion CreateRootNode

        #region CreateEmptyRootNode
        public static JsonObjectVM CreateEmptyRootNode(TranslationConfigVM owner)
            => new(owner);
        #endregion CreateEmptyRootNode

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

        #region CreateSubNode
        public JsonObjectVM CreateSubNode(string name)
        {
            var objectVM = new JsonObjectVM(this, name, new());
            SubNodes.Add(objectVM);
            return objectVM;
        }
        #endregion CreateSubNode

        #region RemoveSubNode
        public void RemoveSubNode(int subNodeIndex)
        {
            if (subNodeIndex < 0 || subNodeIndex >= SubNodes.Count)
                throw new ArgumentOutOfRangeException(nameof(subNodeIndex), subNodeIndex, $"The specified index {subNodeIndex} does not exist! Expected an index in range: (0 >= x < {SubNodes.Count - 1})");

            SubNodes.RemoveAt(subNodeIndex);
        }
        public void RemoveSubNode(JsonObjectVM subNode)
        {
            ArgumentNullException.ThrowIfNull(subNode, nameof(subNode));

            var index = SubNodes.IndexOf(subNode);

            if (index == -1)
                throw new InvalidOperationException($"The specified sub node \"{subNode.Name}\" is not a child of this node \"{Name}\"!");

            RemoveSubNode(index);
        }
        public void RemoveSubNode(string subNodeName, StringComparison stringComparison = StringComparison.Ordinal)
        {
            var subNode = FindSubNodeWithName(subNodeName, stringComparison);
            
            if (subNode == null)
                throw new InvalidOperationException($"The specified sub node \"{subNodeName}\" is not a child of this node \"{Name}\"!");

            RemoveSubNode(subNode);
        }
        #endregion RemoveSubNode

        #region TryRemoveSubNode
        public bool TryRemoveSubNode(int subNodeIndex)
        {
            if (subNodeIndex < 0 || subNodeIndex >= SubNodes.Count)
                return false;
            SubNodes.RemoveAt(subNodeIndex);
            return true;
        }
        public bool TryRemoveSubNode(JsonObjectVM subNode)
        {
            var index = SubNodes.IndexOf(subNode);

            if (index == -1) return false;

            return TryRemoveSubNode(index);
        }
        public bool TryRemoveSubNode(string subNodeName, StringComparison stringComparison = StringComparison.Ordinal)
        {
            var subNode = FindSubNodeWithName(subNodeName, stringComparison);

            if (subNode == null) return false;

            return TryRemoveSubNode(subNode);
        }
        #endregion TryRemoveSubNode

        #region CreateValue
        public JsonValueVM CreateValue(string content)
        {
            var valueVM = new JsonValueVM(this, Owner.LanguageName, content);
            Values.Add(valueVM);
            return valueVM;
        }
        public JsonValueVM CreateValue() => CreateValue(string.Empty);
        #endregion CreateValue

        #region FindValueWithName
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
        #endregion FindValueWithName

        #region FindSubNodeWithName
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
        #endregion FindSubNodeWithName

        #region ToJObject
        /// <summary>
        /// Creates a new <see cref="Newtonsoft.Json.Linq.JObject"/> instance containing the current state of this <see cref="JsonObjectVM"/> instance.
        /// </summary>
        public JObject ToJObject()
        {
            JObject jObject = new();

            foreach (var valueVM in Values)
            {
                jObject.Add(((JsonValueVM)valueVM).ToJProperty());
            }
            foreach (var subNode in SubNodes)
            {
                jObject.Add(subNode.ToJProperty()); //< RECURSE
            }

            return jObject;
        }
        #endregion ToJObject

        #region ToJProperty
        public override JProperty ToJProperty() => !IsRootNode ? new JProperty(Name!, ToJObject()) : throw new InvalidOperationException("Cannot serialize the root node as a JSON Property!");
        #endregion ToJProperty

        #endregion Methods
    }
    [DebuggerDisplay("\"{Name}\": \"{Content}\"")]
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

        #region ToJProperty
        /// <summary>
        /// Creates a new <see cref="JProperty"/> instance containing the current state of this <see cref="JsonValueVM"/> instance.
        /// </summary>
        public override JProperty ToJProperty() => new(Name!, Content);
        #endregion ToJProperty
    }
    public class JsonErrorVM : JsonNodeVM
    {
        #region Constructor
        public JsonErrorVM(JsonNodeVM parent, string name, JToken? jToken) : base(parent, name, jToken!)
        {
        }
        #endregion Constructor

        public override JProperty ToJProperty() => throw new NotImplementedException();
    }
    [DebuggerDisplay("FileName = {FileName}, Path = {OriginalFilePath}")]
    [JsonConverter(typeof(TranslationConfigJsonConverter))]
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

            // get the locale id
            var extensionStartPos = FileName.IndexOf(".loc.json");
            if (extensionStartPos != 2)
            {
                FLog.Error($"Translation config \"{filePath}\" is invalid; filename \"{FileName}\" is not in the correct format.");
                LocaleID = null!;
            }
            else
            {
                LocaleID = FileName[..2];
            }

            // get the language name & display name, and remove the node
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
                    _languageID = languageNameValue.Name!;
                    LanguageDisplayName = languageNameValue.Content!;
                }
                RootNode.SubNodes.Remove(languageNameNode);
            }
        }
        #endregion Constructor

        #region Properties
        public string FileName { get; set; }
        public string LocaleID { get; set; }
        public string OriginalFilePath { get; }
        public string NewFilePath { get; set; } = string.Empty;
        public bool IndentOutput { get; set; } = true;
        public string? DirectoryPath { get; }
        /// <summary>
        /// Gets whether this instance was loaded from an actual file in the filesystem or just from serialized json data.
        /// </summary>
        public bool IsFromActualFile { get; }
        public JsonObjectVM RootNode { get; }
        /// <summary>
        /// Gets or sets the language name used as the key for all translation strings.
        /// </summary>
        public string LanguageName
        {
            get => _languageID;
            set
            {
                _languageID = value.Trim();
                NotifyPropertyChanged();

                SetLanguageNameRecursively(RootNode, _languageID);
            }
        }
        private string _languageID = string.Empty;
        /// <summary>
        /// Gets or sets the language display name shown in the UI.
        /// </summary>
        public string LanguageDisplayName { get; set; } = string.Empty;
        #endregion Properties

        #region Methods

        #region SetLanguageNameRecursively
        /// <summary>
        /// Recursively sets the language name of all values in the specified <paramref name="node"/> and all of its subnodes.
        /// </summary>
        private static void SetLanguageNameRecursively(JsonObjectVM node, string languageName)
        {
            foreach (var valueVM in node.Values)
            {
                valueVM.Name = languageName;
            }
            foreach (var subNode in node.SubNodes)
            {
                SetLanguageNameRecursively(subNode, languageName); //< RECURSE
            }
        }
        #endregion SetLanguageNameRecursively

        #region SaveToFile
        public static bool SaveToFile(TranslationConfigVM inst, string path, Formatting formatting)
        {
            string serialized;
            try
            {
                serialized = JsonConvert.SerializeObject(inst, Formatting.Indented);
            }
            catch (Exception ex)
            {
                FLog.Error($"An exception occurred while serializing config \"{inst.FileName}\"{(inst.IsFromActualFile ? $" ({inst.OriginalFilePath})" : "")}:", ex);
#if DEBUG
                throw;
#else
                return false;
#endif
            }

            try
            {
                File.WriteAllText(path, serialized, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                FLog.Error($"Failed to write translation config \"{inst.FileName}\"{(inst.IsFromActualFile ? $" ({inst.OriginalFilePath})" : "")} to \"{path}\" due to an exception:", ex);
#if DEBUG
                throw;
#else
                return false;
#endif
            }
        }
        public bool SaveToFile()
            => SaveToFile(this, NewFilePath, IndentOutput ? Formatting.Indented : Formatting.None);
        #endregion SaveToFile

        #region OverwriteFile
        /// <summary>
        /// Saves this <see cref="TranslationConfigVM"/> instance to the original file it was loaded from, overwriting it.
        /// </summary>
        public bool OverwriteFile()
        {
            if (!IsFromActualFile)
                throw new InvalidOperationException($"Cannot save translation config \"{FileName}\" to original file because it is not backed by an actual file!");

            return SaveToFile(this, OriginalFilePath, IndentOutput ? Formatting.Indented : Formatting.None);
        }
        #endregion OverwriteFile

        #region ReloadFromFile
        public bool ReloadFromFile()
        {
            if (!IsFromActualFile)
                throw new InvalidOperationException($"Cannot reload translation config \"{FileName}\" from file because it is not backed by an actual file!");
            // read the file contents
            string content;
            try
            {
                content = File.ReadAllText(OriginalFilePath);
            }
            catch (Exception ex)
            {
                FLog.Error($"An exception occurred while reading file \"{OriginalFilePath}\":", ex);
#if DEBUG
                throw;
#else
                return false;
#endif
            }
            // deserialize the file contents into a new instance
            TranslationConfigVM newInst;
            try
            {
                newInst = new(OriginalFilePath, (JObject)JsonConvert.DeserializeObject(content!)!);
            }
            catch (Exception ex)
            {
                FLog.Error($"An exception occurred while deserializing file \"{OriginalFilePath}\":", ex);
#if DEBUG
                throw;
#else
                return false;
#endif
            }
            // copy the new instance's properties into this instance
            var mapper = new UltraMapper.Mapper();
            mapper.Map(newInst, this);
            return true;
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
    public class CustomCommand : ICommand
    {
        public CustomCommand(Action<object?> executed, Func<object?, bool> canExecute)
        {
            _executed = executed;
            _canExecute = canExecute;
        }
        public CustomCommand(Action<object?> executed)
        {
            _executed = executed;
            _canExecute = o => true;
        }

        private readonly Action<object?> _executed;
        private readonly Func<object?, bool> _canExecute;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute(parameter);
        public void Execute(object? parameter) => _executed(parameter);
    }
}
