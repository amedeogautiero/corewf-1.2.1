// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace System.Activities.Runtime.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    //https://github.com/UiPath/corewf/issues/29
    //[KnownType(typeof(ActivityExecutor))]
    ////[KnownType(typeof(Variable<int>.VariableLocation))]
    //[KnownType(typeof(Variable<string>.VariableLocation))]
    //[KnownType(typeof(System.Activities.ActivityInstance))]
    //[KnownType(typeof(Variable<System.Boolean>.VariableLocation))]
    //[KnownType(typeof(System.Activities.Location<System.String>))]
    //[KnownType(typeof(Variable<System.Activities.Statements.Pick.PickState>.VariableLocation))]
    //[KnownType(typeof(System.Activities.Location<int>.ReferenceLocation))]
    

    [DataContract]
    public sealed class InstanceValue
    {
        private readonly static InstanceValue s_deletedValue = new InstanceValue();

        public InstanceValue(object value)
            : this(value, InstanceValueOptions.None)
        {
        }

        public InstanceValue(object value, InstanceValueOptions options)
        {
            Value = value;
            Options = options;
        }

        private InstanceValue()
        {
            Value = this;
        }

        public object Value { get; private set; }

        public InstanceValueOptions Options { get; private set; }

        public bool IsDeletedValue
        {
            get
            {
                return object.ReferenceEquals(this, InstanceValue.DeletedValue);
            }
        }

        public static InstanceValue DeletedValue
        {
            get
            {
                return InstanceValue.s_deletedValue;
            }
        }

        [DataMember(Name = "Value", EmitDefaultValue = false)]
        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, //Justification = "Called from Serialization")]
        internal object SerializedValue
        {
            get
            {
                return this.Value;
            }
            set
            {
                this.Value = value;
            }
        }

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, //Justification = "Called from Serialization")]
        internal InstanceValueOptions SerializedOptions
        {
            get
            {
                return this.Options;
            }
            set
            {
                this.Options = value;
            }
        }
    }
}
