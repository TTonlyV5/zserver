namespace ZMap.SLD;

public class NamespaceIgnorantXmlTextReader(Stream stream) : XmlTextReader(stream)
{
    public override string NamespaceURI => "";
}