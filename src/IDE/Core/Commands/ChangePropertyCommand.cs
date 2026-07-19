using MonoGameMaker.Runtime;

namespace MonoGameMaker.IDE.Core
{
    public class ChangePropertyCommand : IEditorCommand
    {
        private readonly SceneSerializer.EntityInstance _node;
        private readonly string _propertyName;
        private readonly object _oldValue;
        private readonly object _newValue;
        private readonly string? _customPropertyKey;

        public ChangePropertyCommand(SceneSerializer.EntityInstance node, string propertyName, object oldValue, object newValue, string? customPropertyKey = null)
        {
            _node = node;
            _propertyName = propertyName;
            _oldValue = oldValue;
            _newValue = newValue;
            _customPropertyKey = customPropertyKey;
        }

        public void Execute()
        {
            ApplyValue(_newValue);
        }

        public void Undo()
        {
            ApplyValue(_oldValue);
        }

        private void ApplyValue(object value)
        {
            if (_propertyName == "x")
            {
                _node.x = (float)value;
            }
            else if (_propertyName == "y")
            {
                _node.y = (float)value;
            }
            else if (_propertyName == "CustomProperties" && _customPropertyKey != null)
            {
                _node.CustomProperties[_customPropertyKey] = (string)value;
            }
        }
    }
}
