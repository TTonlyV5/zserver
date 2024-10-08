namespace ZMap.SLD.Filter;

/// <remarks/>
[Serializable]
[XmlType]
public class LowerBoundaryType
{
    /// <remarks/>
    [XmlElement("Add", typeof(Add))]
    [XmlElement("Div", typeof(Div))]
    [XmlElement("Function", typeof(FunctionType1))]
    [XmlElement("Literal", typeof(LiteralType))]
    [XmlElement("Mul", typeof(Mul))]
    [XmlElement("PropertyName", typeof(PropertyNameType))]
    [XmlElement("Sub", typeof(Sub))]
    [XmlElement("expression", typeof(ExpressionType))]
    [XmlChoiceIdentifier("ItemElementName")]
    public ExpressionType Item { get; set; }

    /// <remarks/>
    [XmlIgnore]
    [XmlElement("ItemElementName")]
    public LowerBoundaryChoiceType ItemElementName { get; set; }

    [Serializable]
    [XmlType]
    public enum LowerBoundaryChoiceType
    {
        /// <remarks/>
        Add,

        /// <remarks/>
        Div,

        /// <remarks/>
        Function,

        /// <remarks/>
        Literal,

        /// <remarks/>
        Mul,

        /// <remarks/>
        PropertyName,

        /// <remarks/>
        Sub,

        /// <remarks/>
        expression,
    }
}