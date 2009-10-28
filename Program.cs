using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;

namespace InlineApplicationConfig
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var sources = args.Where(File.Exists);
      if (!sources.Any())
      {
        Console.WriteLine("InlineApplicationConfig <source> [destiny]");
        return;
      }
      var source = args.First();
      var destiny = args.Last();
      var finalName = destiny;
      if (source == destiny)
      {
        destiny += "-temp";
      }
      Console.WriteLine("{0} -> {1}", source, finalName);
      using (var writer = new XmlTextWriter(File.CreateText(destiny)))
      {
        writer.Formatting = Formatting.Indented;
        Slurp(writer, source, true);
      }
      if (finalName != destiny)
      {
        if (File.Exists(finalName)) File.Delete(finalName);
        File.Move(destiny, finalName);
      }
    }

    static string GetIncludePath(string workingDirectory, string attributeValue)
    {
      return Path.Combine(workingDirectory, attributeValue.Replace('\\', Path.DirectorySeparatorChar));
    }

    static void Slurp(XmlWriter writer, string path, bool topLevel)
    {
      using (var stream = File.OpenRead(path))
      {
        var workingDirectory = Path.GetDirectoryName(path);
        var reader = XmlReader.Create(stream);
        if (topLevel)
        {
          writer.WriteStartDocument();
        }
        while (reader.Read())
        {
          switch (reader.NodeType)
          {
            case XmlNodeType.Element:
              var writeEnd = reader.IsEmptyElement;
              var wroteStart = false;
              var node = new { reader.Prefix, reader.LocalName, reader.NamespaceURI };
              if (reader.MoveToFirstAttribute())
              {
                do
                {
                  var info = reader.SchemaInfo;
                  if (!reader.IsDefault && ((info == null) || !info.IsDefault))
                  {
                    if (reader.LocalName == "configSource")
                    {
                      Slurp(writer, GetIncludePath(workingDirectory, reader.Value), false);
                      writeEnd = false;
                      continue;
                    }
                    if (!wroteStart)
                    {
                      writer.WriteStartElement(node.Prefix, node.LocalName, node.NamespaceURI);
                      wroteStart = true;
                    }
                    writer.WriteStartAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                    while (reader.ReadAttributeValue())
                    {
                      if (reader.NodeType == XmlNodeType.EntityReference)
                      {
                        writer.WriteEntityRef(reader.Name);
                      }
                      else
                      {
                        writer.WriteString(reader.Value);
                      }
                    }
                    writer.WriteEndAttribute();
                  }
                }
                while (reader.MoveToNextAttribute());
              }
              else
              {
                writer.WriteStartElement(node.Prefix, node.LocalName, node.NamespaceURI);
              }
              if (writeEnd)
              {
                writer.WriteEndElement();
              }
              break;
            case XmlNodeType.Text:
              writer.WriteValue(reader.Value);
              break;
            case XmlNodeType.Whitespace:
              /*
              if (reader.Depth > 0)
              {
                writer.WriteValue(reader.Value);
              }
              */
              break;
            case XmlNodeType.EndElement:
              writer.WriteEndElement();
              break;
            case XmlNodeType.CDATA:
              writer.WriteCData(reader.Value);
              break;
            case XmlNodeType.XmlDeclaration:
              break;
            default:
              throw new NotSupportedException();
          }
        }
      }
    }
  }
}
