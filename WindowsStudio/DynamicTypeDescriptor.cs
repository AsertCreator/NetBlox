using System.ComponentModel;
using System.Drawing.Design;
// thank you creative commons
namespace NetBlox.Studio;

#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8769 // Nullability of reference types in type of parameter doesn't match implemented member (possibly because of nullability attributes).
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
#pragma warning disable CS8768 // Nullability of reference types in return type doesn't match implemented member (possibly because of nullability attributes).
public sealed class DynamicTypeDescriptor : ICustomTypeDescriptor, INotifyPropertyChanged
{
	private Type _type;
	private AttributeCollection _attributes;
	private TypeConverter _typeConverter;
	private Dictionary<Type, object> _editors;
	private EventDescriptor _defaultEvent;
	private PropertyDescriptor _defaultProperty;
	private EventDescriptorCollection _events;

	public event PropertyChangedEventHandler? PropertyChanged;

	private DynamicTypeDescriptor()
	{
	}

	public DynamicTypeDescriptor(Type type)
	{
		_type = type ?? throw new ArgumentNullException(nameof(type));
		_typeConverter = TypeDescriptor.GetConverter(type);
		_defaultEvent = TypeDescriptor.GetDefaultEvent(type)!;
		_defaultProperty = TypeDescriptor.GetDefaultProperty(type)!;
		_events = TypeDescriptor.GetEvents(type);

		List<PropertyDescriptor> normalProperties = [];
		OriginalProperties = TypeDescriptor.GetProperties(type);
		foreach (PropertyDescriptor property in OriginalProperties)
		{
			if (!property.IsBrowsable)
				continue;

			normalProperties.Add(property);

		}
		Properties = new PropertyDescriptorCollection([.. normalProperties]);

		_attributes = TypeDescriptor.GetAttributes(type);

		_editors = [];
		object editor = TypeDescriptor.GetEditor(type, typeof(UITypeEditor));
		if (editor != null)
		{
			_editors.Add(typeof(UITypeEditor), editor);
		}
		editor = TypeDescriptor.GetEditor(type, typeof(ComponentEditor));
		if (editor != null)
		{
			_editors.Add(typeof(ComponentEditor), editor);
		}
		editor = TypeDescriptor.GetEditor(type, typeof(InstanceCreationEditor));
		if (editor != null)
		{
			_editors.Add(typeof(InstanceCreationEditor), editor);
		}
	}

	public T GetPropertyValue<T>(string name, T defaultValue)
	{
		ArgumentNullException.ThrowIfNull(name);

		foreach (PropertyDescriptor pd in Properties)
		{
			if (pd.Name == name)
			{
				try
				{
					return (T)Convert.ChangeType(pd.GetValue(Component), typeof(T));
				}
				catch
				{
					return defaultValue;
				}
			}
		}
		return defaultValue;
	}

	public void SetPropertyValue(string name, object value)
	{
		ArgumentNullException.ThrowIfNull(name);

		foreach (PropertyDescriptor pd in Properties)
		{
			if (pd.Name == name)
			{
				pd.SetValue(Component, value);
				break;
			}
		}
	}

	internal void OnValueChanged(PropertyDescriptor prop)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop.Name));
	}

	internal static T GetAttribute<T>(AttributeCollection attributes) where T : Attribute
	{
		if (attributes == null)
			return null;

		foreach (Attribute att in attributes)
		{
			if (typeof(T).IsAssignableFrom(att.GetType()))
				return (T)att;
		}
		return null;
	}

	public sealed class DynamicProperty : PropertyDescriptor, INotifyPropertyChanged
	{
		private readonly Type _type;
		private readonly bool _hasDefaultValue;
		private readonly object _defaultValue;
		private readonly PropertyDescriptor _existing;
		private readonly DynamicTypeDescriptor _descriptor;
		private Dictionary<Type, object> _editors;
		private bool? _readOnly;
		private bool? _browsable;
		private string _displayName;
		private string _description;
		private string _category;
		private readonly List<Attribute> _attributes = [];

		public event PropertyChangedEventHandler? PropertyChanged;

		internal DynamicProperty(DynamicTypeDescriptor descriptor, Type type, object value, string name, Attribute[] attrs)
			: base(name, attrs)
		{
			_descriptor = descriptor;
			_type = type;
			Value = value;
			DefaultValueAttribute def = DynamicTypeDescriptor.GetAttribute<DefaultValueAttribute>(Attributes);
			if (def == null)
			{
				_hasDefaultValue = false;
			}
			else
			{
				_hasDefaultValue = true;
				_defaultValue = def.Value!;
			}
			if (attrs != null)
			{
				foreach (Attribute att in attrs)
				{
					_attributes.Add(att);
				}
			}
		}

		internal static Attribute[] GetAttributes(PropertyDescriptor existing)
		{
			List<Attribute> atts = new ();
			foreach (Attribute a in existing.Attributes)
			{
				atts.Add(a);
			}
			return [.. atts];
		}

		internal DynamicProperty(DynamicTypeDescriptor descriptor, PropertyDescriptor existing, object component)
			: this(descriptor, existing.PropertyType, existing.GetValue(component), existing.Name, GetAttributes(existing)) => _existing = existing;

		public void RemoveAttributesOfType<T>() where T : Attribute
		{
			List<Attribute> remove = [];
			foreach (Attribute att in _attributes)
			{
				if (typeof(T).IsAssignableFrom(att.GetType()))
				{
					remove.Add(att);
				}
			}

			foreach (Attribute att in remove)
			{
				_attributes.Remove(att);
			}
		}

		public IList<Attribute> AttributesList => _attributes;

		public override AttributeCollection Attributes => new([.. _attributes]);

		public object Value { get; set; }

		public override bool CanResetValue(object component) => _existing != null ? _existing.CanResetValue(component) : _hasDefaultValue;

		public override Type ComponentType => _existing != null ? _existing.ComponentType : typeof(object);

		public override object GetValue(object component) => _existing != null ? _existing.GetValue(component) : Value;

		public override string Category => _category ?? base.Category;

		public void SetCategory(string category) => _category = category;

		public override string Description => _description ?? base.Description;

		public void SetDescription(string description) => _description = description;

		public override string DisplayName
		{
			get
			{
				if (_displayName != null)
					return _displayName;

				return _existing != null ? _existing.DisplayName : base.DisplayName;
			}
		}

		public void SetDisplayName(string displayName) => _displayName = displayName;

		public override bool IsBrowsable => _browsable ?? base.IsBrowsable;

		public void SetBrowsable(bool browsable) => _browsable = browsable;

		public override bool IsReadOnly
		{
			get
			{
				if (_readOnly.HasValue)
					return _readOnly.Value;

				if (_existing != null)
					return _existing.IsReadOnly;

				ReadOnlyAttribute att = DynamicTypeDescriptor.GetAttribute<ReadOnlyAttribute>(Attributes);
				return att != null && att.IsReadOnly;
			}
		}

		public void SetIsReadOnly(bool readOnly) => _readOnly = readOnly;

		public override Type PropertyType => _existing != null ? _existing.PropertyType : _type;

		public override void ResetValue(object component)
		{
			if (_existing != null)
			{
				_existing.ResetValue(component);
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
				_descriptor.OnValueChanged(this);
				return;
			}

			if (CanResetValue(component))
			{
				Value = _defaultValue;
				_descriptor.OnValueChanged(this);
			}
		}
		public override void SetValue(object component, object value)
		{
			if (_existing != null)
			{
				_existing.SetValue(component, value);
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
				_descriptor.OnValueChanged(this);
				return;
			}

			Value = value;
			_descriptor.OnValueChanged(this);
		}

		public override bool ShouldSerializeValue(object component) => _existing != null && _existing.ShouldSerializeValue(component);

		public override object GetEditor(Type editorBaseType)
		{
			ArgumentNullException.ThrowIfNull(editorBaseType);

			if (_editors != null)
			{
				if (_editors.TryGetValue(editorBaseType, out object type) && (type != null))
					return type;
			}
			return base.GetEditor(editorBaseType);
		}

		public void SetEditor(Type editorBaseType, object obj)
		{
			ArgumentNullException.ThrowIfNull(editorBaseType);

			if (_editors == null)
			{
				if (obj == null)
					return;

				_editors = [];
			}
			if (obj == null)
			{
				_editors.Remove(editorBaseType);
			}
			else
			{
				_editors[editorBaseType] = obj;
			}
		}
	}

	public PropertyDescriptor AddProperty(Type type, string name, object value, string displayName, string description, string category, bool hasDefaultValue, object defaultValue, bool readOnly) => AddProperty(type, name, value, displayName, description, category, hasDefaultValue, defaultValue, readOnly, null);

	public PropertyDescriptor AddProperty(
		Type type,
		string name,
		object value,
		string displayName,
		string description,
		string category,
		bool hasDefaultValue,
		object defaultValue,
		bool readOnly,
		Type uiTypeEditor)
	{
		ArgumentNullException.ThrowIfNull(type);
		ArgumentNullException.ThrowIfNull(name);

		List<Attribute> atts = [];
		if (!string.IsNullOrEmpty(displayName))
		{
			atts.Add(new DisplayNameAttribute(displayName));
		}

		if (!string.IsNullOrEmpty(description))
		{
			atts.Add(new DescriptionAttribute(description));
		}

		if (!string.IsNullOrEmpty(category))
		{
			atts.Add(new CategoryAttribute(category));
		}

		if (hasDefaultValue)
		{
			atts.Add(new DefaultValueAttribute(defaultValue));
		}

		if (uiTypeEditor != null)
		{
			atts.Add(new EditorAttribute(uiTypeEditor, typeof(UITypeEditor)));
		}

		if (readOnly)
		{
			atts.Add(new ReadOnlyAttribute(true));
		}

		DynamicProperty property = new(this, type, value, name, [.. atts]);
		AddProperty(property);
		return property;
	}

	public void RemoveProperty(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		List<PropertyDescriptor> remove = [];
		foreach (PropertyDescriptor pd in Properties)
		{
			if (pd.Name == name)
			{
				remove.Add(pd);
			}
		}

		foreach (PropertyDescriptor pd in remove)
		{
			Properties.Remove(pd);
		}
	}

	public void AddProperty(PropertyDescriptor property)
	{
		ArgumentNullException.ThrowIfNull(property);

		Properties.Add(property);
	}

	public override string ToString() => base.ToString() + " (" + Component + ")";

	public PropertyDescriptorCollection OriginalProperties { get; private set; }
	public PropertyDescriptorCollection Properties { get; private set; }

	public DynamicTypeDescriptor FromComponent(object component)
	{
		ArgumentNullException.ThrowIfNull(component);

		if (!_type.IsAssignableFrom(component.GetType()))
			throw new ArgumentException(null, nameof(component));

		DynamicTypeDescriptor desc = new()
		{
			_type = _type,
			Component = component,

			// shallow copy on purpose
			_typeConverter = _typeConverter,
			_editors = _editors,
			_defaultEvent = _defaultEvent,
			_defaultProperty = _defaultProperty,
			_attributes = _attributes,
			_events = _events,
			OriginalProperties = OriginalProperties
		};

		// track values
		List<PropertyDescriptor> properties = [];
		foreach (PropertyDescriptor pd in Properties)
		{
			DynamicProperty ap = new(desc, pd, component);
			properties.Add(ap);
		}

		desc.Properties = new PropertyDescriptorCollection([.. properties]);
		return desc;
	}

	public object Component { get; private set; }
	public string ClassName { get; set; }
	public string ComponentName { get; set; }

	AttributeCollection ICustomTypeDescriptor.GetAttributes() => _attributes;

	string ICustomTypeDescriptor.GetClassName()
	{
		if (ClassName != null)
			return ClassName;

		if (Component != null)
			return Component.GetType().Name;

		return _type?.Name;
	}

	string ICustomTypeDescriptor.GetComponentName() => ComponentName ?? (Component?.ToString());

	TypeConverter ICustomTypeDescriptor.GetConverter() => _typeConverter;

	EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => _defaultEvent;

	PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => _defaultProperty;

	object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => _editors.TryGetValue(editorBaseType, out object editor) ? editor : null;

	EventDescriptorCollection? ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => _events;

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => _events;

	PropertyDescriptorCollection? ICustomTypeDescriptor.GetProperties(Attribute[] attributes) => Properties;

	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => Properties;

	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => Component;
}

#pragma warning restore CS8768 // Nullability of reference types in return type doesn't match implemented member (possibly because of nullability attributes).
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
#pragma warning restore CS8769 // Nullability of reference types in type of parameter doesn't match implemented member (possibly because of nullability attributes).
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8603 // Possible null reference return.
