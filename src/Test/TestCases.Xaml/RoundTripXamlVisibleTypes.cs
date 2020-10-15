﻿// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;
using TestCases.Xaml.Common.InstanceCreator;
using TestObjects.XamlTestDriver;
using Xunit;

namespace TestCases.Xaml
{
    public class RoundTripXamlVisibleTypes
    {
        public static IEnumerable<object[]> SysActTypes
        {
            get
            {
                return new object[][] {
                    new object[] { typeof(VisualBasicSettings) },
                    new object[] { typeof(VisualBasicReference<int>) },
                    new object[] { typeof(VisualBasicValue<int>) },
                    new object[] { typeof(CSharpReference<int>) },
                    new object[] { typeof(CSharpValue<int>) },
                    new object[] { typeof(ActivityBuilder) },
                    new object[] { typeof(Add<int, int, int>) },
                    new object[] { typeof(And<bool, bool, bool>) },
                    new object[] { typeof(ArrayItemReference<int>) },
                    new object[] { typeof(ArrayItemValue<int>) },
                    new object[] { typeof(As<object, object>) },
                    new object[] { typeof(Cast<object, object>) },
                    new object[] { typeof(Divide<int, int, int>) },
                    new object[] { typeof(Equal<string, string, bool>) },
                    new object[] { typeof(FieldReference<int, int>) },
                    new object[] { typeof(FieldValue<int, int>) },
                    new object[] { typeof(GreaterThan<int, int, bool>) },
                    new object[] { typeof(GreaterThanOrEqual<int, int, bool>) },
                    new object[] { typeof(IndexerReference<string, int>) },
                    new object[] { typeof(InvokeFunc<int>) },
                    new object[] { typeof(InvokeFunc<int,int>) },
                    new object[] { typeof(InvokeFunc<int,int,int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int, int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int, int, int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int, int, int, int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int, int, int, int, int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int, int, int, int, int, int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int, int, int, int, int, int, int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int, int, int, int, int, int, int, int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int, int, int, int, int, int, int, int, int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int, int, int, int, int, int, int, int, int, int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int, int, int, int, int, int, int, int, int, int, int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>) },
                    new object[] { typeof(InvokeFunc<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>) },
                    new object[] { typeof(InvokeMethod<int>) },
                    new object[] { typeof(LessThan<int, int, int>) },
                    new object[] { typeof(LessThanOrEqual<int, int, int>) },
                    new object[] { typeof(Literal<int>) },
                    new object[] { typeof(MultidimensionalArrayItemReference<int>) },
                    new object[] { typeof(Multiply<int, int, int>) },
                    new object[] { typeof(New<int>) },
                    new object[] { typeof(NewArray<int>) },
                    new object[] { typeof(Not<int, int>) },
                    new object[] { typeof(NotEqual<int, int, int>) },
                    new object[] { typeof(Or<int, int, int>) },
                    new object[] { typeof(OrElse) },
                    new object[] { typeof(PropertyReference<int, int>) },
                    new object[] { typeof(PropertyValue<int, int>) },
                    new object[] { typeof(Subtract<int, int, int>) },
                    new object[] { typeof(ValueTypeFieldReference<int, int>) },
                    new object[] { typeof(ValueTypeIndexerReference<int, int>) },
                    new object[] { typeof(ValueTypePropertyReference<int, int>) },
                    new object[] { typeof(VariableReference<int>) },
                    new object[] { typeof(VariableValue<int>) },
                    new object[] { typeof(AddToCollection<int>) },
                    new object[] { typeof(Assign) },
                    new object[] { typeof(Assign<int>) },
                    new object[] { typeof(Catch) },
                    new object[] { typeof(Catch<Exception>) },
                    new object[] { typeof(ClearCollection<int>) },
                    new object[] { typeof(Delay) },
                    new object[] { typeof(DoWhile) },
                    new object[] { typeof(DurableTimerExtension) },
                    new object[] { typeof(ExistsInCollection<int>) },
                    new object[] { typeof(Flowchart) },
                    new object[] { typeof(FlowDecision) },
                    new object[] { typeof(FlowNode) },
                    new object[] { typeof(FlowStep) },
                    new object[] { typeof(FlowSwitch<int>) },
                    new object[] { typeof(ForEach<int>) },
                    new object[] { typeof(If) },
                    new object[] { typeof(InvokeMethod) },
                    new object[] { typeof(Parallel) },
                    new object[] { typeof(ParallelForEach<int>) },
                    new object[] { typeof(Persist) },
                    new object[] { typeof(Pick) },
                    new object[] { typeof(PickBranch) },
                    new object[] { typeof(RemoveFromCollection<int>) },
                    new object[] { typeof(Rethrow) },
                    new object[] { typeof(Sequence) },
                    new object[] { typeof(Switch<int>) },
                    new object[] { typeof(TerminateWorkflow) },
                    new object[] { typeof(Throw) },
                    new object[] { typeof(TimerExtension) },
                    new object[] { typeof(TryCatch) },
                    new object[] { typeof(While) },
                    new object[] { typeof(WorkflowTerminatedException) },
                    new object[] { typeof(WriteLine) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(SysActTypes))]
        public void RoundTripObject(Type type)
        {
            Object[] instances = new Object[3];
            Object obj = null;
            DateTime now = DateTime.Now;
            int seed = 10000 * now.Year + 100 * now.Month + now.Day;
            Random rndGen = new Random(seed);

            obj = InstanceCreator.CreateInstanceOf(type, rndGen);
            if (obj != null && !(obj is MarkupExtension))
            {
                Object returnedObj = null;
                returnedObj = XamlTestDriver.RoundTripAndCompareObjects(obj, "CacheId", "Implementation", "ImplementationVersion");
                Assert.NotNull(returnedObj);
            }
        }

        [Theory]
        [InlineData(typeof(AndAlso))]
        public void DynamicImplCantSerialize(Type type)
        {
            try
            {
                RoundTripObject(type);
            }
            catch (System.Xaml.XamlObjectReaderException exc)
            {
                Assert.NotNull(exc.InnerException);
                Assert.Equal(typeof(System.NotSupportedException), exc.InnerException.GetType());
            }
        }

        [Theory]
        [InlineData(typeof(TypeConverters))]
        [InlineData(typeof(OtherXaml))]
        public void Converters(Type type)
        {
            var typeNames = type.GetFields().ToDictionary(f=>f.Name, f=>f.GetRawConstantValue().ToString());
            var types = typeNames.Values.Select(Type.GetType).ToArray();
            foreach (var (typeName, typeObject) in typeNames.Zip(types, (typeName, typeObject)=>(typeName, typeObject)))
            {
                typeObject.Name.ShouldBe(typeName.Key);
                if (typeObject != typeof(Activity))
                {
                    typeObject.Namespace.ShouldBe("System.Activities.XamlIntegration");
                    typeObject.Assembly.ShouldBe(typeof(ActivityBuilder).Assembly);
                }
            }
        }
    }
}
