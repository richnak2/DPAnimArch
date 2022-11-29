﻿using System.Collections.Generic;
using System;
using System.Linq;
using System.Xml;
using System.Text;
using AnimArch.Visualization.Diagrams;
using AnimArch.Visualization.Animating;
using Attribute = AnimArch.Visualization.Diagrams.Attribute;

namespace AnimArch.XMIParsing
{
    public static class XMIParser
    {
        private static List<string> ParseCurrentDiagramElementsIDs()
        {
            //Document
            var xmlDoc = new XmlDocument(); // Create an XML document object
            var encoding = Encoding.GetEncoding("UTF-8");
            //System.IO.StreamReader reader = new System.IO.StreamReader(path, System.Text.Encoding.GetEncoding("Windows-1252"), true);
            var xmlText = System.IO.File.ReadAllText(AnimationData.Instance.GetDiagramPath(), encoding);
            xmlDoc.LoadXml(xmlText); // Load the XML document from the specified file

            var currDiagramID = AnimationData.Instance.diagramId.ToString();
            //pridaj vsetky elementy patriace current otvorenemu diagramu
            var currDiagramElements = new List<string>();
            var diagrams = xmlDoc.GetElementsByTagName("diagrams");
            var d = diagrams[0].ChildNodes;

            foreach (XmlNode diagram in d)
            {
                var diagramNodes = diagram.ChildNodes;

                foreach (XmlNode node in diagramNodes)
                {
                    if (node.Name == "model")
                    {
                        if (node.Attributes?["localID"].Value == currDiagramID == false) break;
                    }

                    if (node.Name != "elements") continue;
                    var diagramElements = node.ChildNodes;
                    currDiagramElements.AddRange(from XmlNode diagramElement in diagramElements
                        select diagramElement.Attributes?["subject"].Value);
                }
            }

            return currDiagramElements;
        }

        public static List<Class> ParseClasses()
        {
            var XMIClassList = new List<Class>();
            var xmlDoc = new XmlDocument(); // Create an XML document object
            // Load the XML document from the specified file
            var encoding = Encoding.GetEncoding("UTF-8");
            var xml = System.IO.File.ReadAllText(AnimationData.Instance.GetDiagramPath(), encoding);
            xmlDoc.LoadXml(xml);
            var currDiagramID = AnimationData.Instance.diagramId.ToString();
            //string currDiagramID = System.IO.File.ReadAllText(currDiagramIDPath);
            var currDiagramElements = ParseCurrentDiagramElementsIDs();

            // Get elements
            // var classNodeList = xmlDoc.GetElementsByTagName("UML:Class");
            // var classIndices = xmlDoc.GetElementsByTagName("UML:DiagramElement");
            var elementClass = xmlDoc.GetElementsByTagName("elements");


            var elementsClass = elementClass[0].ChildNodes;
            //XmlNodeList geometryElements = elementClass[1].ChildNodes; //todo fix for current diagram
            XmlNodeList geometryElements = null;

            for (var i = 1; i < elementClass.Count; i++)
            {
                var parentDiagram = elementClass[i].ParentNode;
                var parentDiagramNodes = parentDiagram?.ChildNodes;
                if (parentDiagramNodes.Cast<XmlNode>().Where(node => node.Name == "model")
                    .Any(node => node.Attributes?["localID"].Value == currDiagramID))
                {
                    geometryElements = elementClass[i].ChildNodes;
                }
            }


            for (var i = 1; i < elementsClass.Count; i++)
            {
                var XMIClass = new Class
                {
                    Type = elementsClass[i].Attributes["xmi:type"].Value
                };
                try
                {
                    XMIClass.Name = elementsClass[i].Attributes["name"].Value;
                }
                catch
                {
                    // ignored
                }

                XMIClass.XmiId = elementsClass[i].Attributes["xmi:idref"].Value;
                //  XMIClassList.Add(XMIClass);
                if (!(XMIClass.Type.Equals("uml:Interface") || XMIClass.Type.Equals("uml:Class"))) continue;

                if (elementsClass[i].HasChildNodes)
                {
                    var test = elementsClass[i].ChildNodes;
                    foreach (XmlNode node in test)
                    {
                        switch (node.Name)
                        {
                            case "attributes":
                            {
                                var attributes = node.ChildNodes;
                                XMIClass.Attributes = new List<Attribute>();

                                foreach (XmlNode attribute in attributes)
                                {
                                    var type = "";
                                    var id = attribute.Attributes["xmi:idref"].Value;
                                    var name = attribute.Attributes["name"].Value;

                                    var attributeAttributes = attribute.ChildNodes;
                                    foreach (XmlNode attributeAttribute in attributeAttributes)
                                    {
                                        if (attributeAttribute.Name == "properties")
                                        {
                                            type = attributeAttribute.Attributes["type"].Value;
                                        }
                                    }

                                    var attr = new Attribute(id, name, type);
                                    XMIClass.Attributes.Add(attr);
                                }

                                break;
                            }
                            case "operations":
                            {
                                var operations = node.ChildNodes;
                                XMIClass.Methods = new List<Method>();

                                foreach (XmlNode operation in operations)
                                {
                                    var name = operation.Attributes["name"].Value;
                                    var id = operation.Attributes["xmi:idref"].Value;
                                    var arguments = new List<string>();

                                    var oper = new Method
                                    {
                                        Name = name,
                                        Id = id
                                    };


                                    var operationProperties = operation.ChildNodes;

                                    foreach (XmlNode operationProperty in operationProperties)
                                    {
                                        switch (operationProperty.Name)
                                        {
                                            //TODO
                                            case "parameters":
                                            {
                                                var parameters = operationProperty.ChildNodes;

                                                var count = 0;
                                                foreach (XmlNode parameter in parameters)
                                                {
                                                    if (count++ <= 0) continue;
                                                    var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                                                    nsmgr.AddNamespace("xmi", "http://schema.omg.org/spec/XMI/2.1");
                                                    var xmiID = parameter.Attributes["xmi:idref"].Value;
                                                    var refnode = xmlDoc.SelectSingleNode(
                                                        "//*[@xmi:id='" + xmiID + "']",
                                                        nsmgr);
                                                    // XmlNode refnode = xmlDoc.SelectSingleNode("//*[@xmi:id='EAID_855A1E81_E810_4e59_B919_A02E42179E4F']", nsmgr);


                                                    var argname = refnode.Attributes["name"].Value;


                                                    var parameterProperties = parameter.ChildNodes;

                                                    arguments.AddRange(from XmlNode parameterProperty in parameter
                                                        where parameterProperty.Name == "properties"
                                                        select parameterProperty.Attributes["type"].Value
                                                        into argpom
                                                        select argpom + " " + argname);
                                                }

                                                //Method oper = new Method(name, id, returnType);   
                                                oper.arguments = arguments;
                                                break;
                                            }
                                            case "type":
                                            {
                                                var returnType = operationProperty.Attributes["type"].Value;
                                                /*Method oper = new Method(name, id, returnType);
                                        XMIClass.Methods.Add(oper);*/
                                                oper.ReturnValue = returnType;

                                                XMIClass.Methods.Add(oper);
                                                break;
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                        }
                    }
                }

                if (currDiagramElements.Contains(XMIClass.XmiId)) XMIClassList.Add(XMIClass);
            }

            if (geometryElements == null)
            {
                return null;
            }

            for (int i = 0; i < geometryElements.Count; i++)
            {
                var subject = geometryElements[i].Attributes["subject"].Value;
                foreach (var item in XMIClassList.Where(item => item.XmiId == subject))
                {
                    item.Geometry = geometryElements[i].Attributes["geometry"].Value;

                    var words = item.Geometry.Split(';');
                    foreach (var word in words)
                    {
                        //aby som nedostal IndexOutOfRangeException... 
                        if (string.IsNullOrEmpty(word)) break;
                        var values = word.Split('=');
                        switch (values[0])
                        {
                            case "Left":
                                item.Left = int.Parse(values[1]);
                                break;
                            case "Top":
                                item.Top = int.Parse(values[1]);
                                break;
                            case "Right":
                                item.Right = int.Parse(values[1]);
                                break;
                            case "Bottom":
                                item.Bottom = int.Parse(values[1]);
                                break;
                        }
                    }
                }
            }

            return XMIClassList;
        }

        public static List<Relation> ParseRelations()
        {
            var connectorClassesList = new List<Relation>();

            var xmlDoc = new XmlDocument(); // Create an XML document object
            var encoding = Encoding.GetEncoding("UTF-8");
            var xmlText = System.IO.File.ReadAllText(AnimationData.Instance.GetDiagramPath(), encoding);
            // XmlTextReader xmlReader = new XmlTextReader(reader);
            xmlDoc.LoadXml(xmlText); // Load the XML document from the specified file
            var currDiagramElements = ParseCurrentDiagramElementsIDs();

            var connectorClass = xmlDoc.GetElementsByTagName("connectors");


            foreach (XmlNode connector in connectorClass)
            {
                if (!connector.HasChildNodes) continue;
                var childNodeList = connector.ChildNodes;
                //prejde vsetky <connector>
                foreach (XmlNode childNode in childNodeList)
                {
                    var xmiConnectorClass = new Relation
                    {
                        ConnectorXmiId = childNode.Attributes["xmi:idref"].Value
                    };

                    if (childNode.HasChildNodes)
                    {
                        var childNodeNextList = childNode.ChildNodes;

                        foreach (XmlNode nodeNext in childNodeNextList)
                        {
                            var name = nodeNext.Name;

                            switch (name)
                            {
                                case "source":
                                    xmiConnectorClass.SourceXmiId = nodeNext.Attributes["xmi:idref"].Value;
                                    xmiConnectorClass.SourceName = nodeNext.Name; //wtf?

                                    if (nodeNext.HasChildNodes)
                                    {
                                        var childNodeSource = nodeNext.ChildNodes;
                                        foreach (XmlNode sourceNode in childNodeSource)
                                        {
                                            //inside <source>
                                            var innerName = sourceNode.Name;
                                            switch (innerName)
                                            {
                                                case "model":
                                                    xmiConnectorClass.SourceModelEaLocalId =
                                                        int.Parse(sourceNode.Attributes["ea_localid"].Value);
                                                    xmiConnectorClass.SourceModelType =
                                                        sourceNode.Attributes["type"].Value; //!!!
                                                    try
                                                    {
                                                        xmiConnectorClass.SourceModelName =
                                                            sourceNode.Attributes["name"].Value;
                                                    }
                                                    catch
                                                    {
                                                        // ignored
                                                    }

                                                    break;
                                                case "role":
                                                    xmiConnectorClass.SourceRoleVisibility =
                                                        sourceNode.Attributes["visibility"].Value;
                                                    xmiConnectorClass.SourceRoleTargetScope =
                                                        sourceNode.Attributes["targetScope"].Value;
                                                    break;
                                                case "type":
                                                    xmiConnectorClass.SourceTypeContainment =
                                                        sourceNode.Attributes["containment"].Value;
                                                    try
                                                    {
                                                        xmiConnectorClass.SourceMultiplicity =
                                                            sourceNode.Attributes["multiplicity"].Value;
                                                    }
                                                    catch
                                                    {
                                                        // ignored
                                                    }

                                                    break;
                                                case "modifiers":
                                                    xmiConnectorClass.SourceModifiersIsOrdered =
                                                        bool.Parse(sourceNode.Attributes["isOrdered"].Value);
                                                    xmiConnectorClass.SourceModifiersChangeable =
                                                        sourceNode.Attributes["changeable"].Value;
                                                    xmiConnectorClass.SourceModifiersisNavigablee =
                                                        bool.Parse(sourceNode.Attributes["isNavigable"].Value);
                                                    break;
                                                case "style":
                                                    xmiConnectorClass.SourceStyleValue =
                                                        sourceNode.Attributes["value"].Value;
                                                    break;
                                            }
                                        }
                                    }

                                    break;
                                case "target":
                                    xmiConnectorClass.TargetXmiId = nodeNext.Attributes["xmi:idref"].Value;
                                    xmiConnectorClass.TargetName = nodeNext.Name;

                                    if (nodeNext.HasChildNodes)
                                    {
                                        var childNodeTarget = nodeNext.ChildNodes;
                                        foreach (XmlNode targetNode in childNodeTarget)
                                        {
                                            //inside <target>
                                            string innerName = targetNode.Name;
                                            switch (innerName)
                                            {
                                                case "model":
                                                    xmiConnectorClass.TargetModelEaLocalId =
                                                        Int32.Parse(targetNode.Attributes["ea_localid"].Value);
                                                    xmiConnectorClass.TargetModelType =
                                                        targetNode.Attributes["type"].Value; //!!!
                                                    try
                                                    {
                                                        xmiConnectorClass.TargetModelName =
                                                            targetNode.Attributes["name"].Value;
                                                    }
                                                    catch
                                                    {
                                                        // ignored
                                                    }

                                                    break;
                                                case "role":
                                                    xmiConnectorClass.TargetRoleVisibility =
                                                        targetNode.Attributes["visibility"].Value;
                                                    xmiConnectorClass.TargetRoleTargetScope =
                                                        targetNode.Attributes["targetScope"].Value;
                                                    break;
                                                case "type":
                                                    xmiConnectorClass.TargetTypeAggregation =
                                                        targetNode.Attributes["aggregation"].Value;
                                                    xmiConnectorClass.TargetTypeContainment =
                                                        targetNode.Attributes["containment"].Value;
                                                    try
                                                    {
                                                        xmiConnectorClass.TargetMultiplicity =
                                                            targetNode.Attributes["multiplicity"].Value;
                                                    }
                                                    catch
                                                    {
                                                        // ignored
                                                    }

                                                    break;
                                                case "modifiers":
                                                    xmiConnectorClass.TargetModifiersIsOrdered =
                                                        bool.Parse(targetNode.Attributes["isOrdered"].Value);
                                                    xmiConnectorClass.TargetModifiersChangeable =
                                                        targetNode.Attributes["changeable"].Value;
                                                    xmiConnectorClass.TargetModifiersisNavigablee =
                                                        bool.Parse(targetNode.Attributes["isNavigable"].Value);
                                                    break;
                                                case "style":
                                                    xmiConnectorClass.TargetStyleValue =
                                                        targetNode.Attributes["value"].Value;
                                                    break;
                                            }
                                        }
                                    }

                                    break;
                                case "model":
                                    xmiConnectorClass.ModelEaLocalId =
                                        int.Parse(nodeNext.Attributes["ea_localid"].Value);
                                    break;
                                case "properties":
                                    xmiConnectorClass.PropertiesEa_type = nodeNext.Attributes["ea_type"].Value;
                                    xmiConnectorClass.ProperitesDirection = nodeNext.Attributes["direction"].Value;
                                    break;
                                case "modifiers":
                                    xmiConnectorClass.ModifiersIsRoot =
                                        bool.Parse(nodeNext.Attributes["isRoot"].Value);
                                    xmiConnectorClass.ModifiersIsLeaf =
                                        bool.Parse(nodeNext.Attributes["isLeaf"].Value);
                                    break;
                                case "appearance":
                                    xmiConnectorClass.AppearanceLinemode = nodeNext.Attributes["linemode"].Value;
                                    xmiConnectorClass.AppearanceLineColor = nodeNext.Attributes["linecolor"].Value;
                                    xmiConnectorClass.AppearanceLinewidth = nodeNext.Attributes["linewidth"].Value;
                                    xmiConnectorClass.AppearanceSeqno = nodeNext.Attributes["seqno"].Value;
                                    xmiConnectorClass.AppearanceHeadStyle = nodeNext.Attributes["headStyle"].Value;
                                    xmiConnectorClass.AppearanceLineStyle = nodeNext.Attributes["lineStyle"].Value;
                                    break;
                                case "labels":
                                    try
                                    {
                                        xmiConnectorClass.Label = nodeNext.Attributes["mt"].Value;
                                    }
                                    catch
                                    {
                                        // ignored
                                    }

                                    break;
                                case "extendedProperties":
                                    break;
                            }
                        }
                    }

                    if ((!xmiConnectorClass.SourceModelType.Equals("Class") &&
                         !xmiConnectorClass.SourceModelType.Equals("Interface")) ||
                        (!xmiConnectorClass.TargetModelType.Equals("Interface") &&
                         !xmiConnectorClass.TargetModelType.Equals("Class"))) continue;
                    if (currDiagramElements.Contains(xmiConnectorClass.ConnectorXmiId))
                    {
                        connectorClassesList.Add(xmiConnectorClass);
                    }
                }
            }

            return connectorClassesList;
        }

        
        
        public static void ParseDiagramIntoXmi()
        {
            var doc = new XmlDocument( );

            var xmlDeclaration = doc.CreateXmlDeclaration( "1.0", "UTF-8", null );
            var root = doc.DocumentElement;
            doc.InsertBefore( xmlDeclaration, root );

            const string xmiUri = "http://schema.omg.org/spec/XMI/2.1";
            const string umlUri = "http://schema.omg.org/spec/UML/2.1";
            var xmIElem = doc.CreateElement( "xmi", "XMI", xmiUri);
            xmIElem.SetAttribute("version", "2.1");
            xmIElem.SetAttribute("xmlns:uml", umlUri);
            xmIElem.SetAttribute("xmlns:xmi", xmiUri);
            doc.AppendChild( xmIElem );
            
            var xmiDocumentation = doc.CreateElement("xmi", "Documentation", xmiUri);
            xmIElem.AppendChild(xmiDocumentation);
            var xmiModel = doc.CreateElement("xmi", "Model", xmiUri);
            xmIElem.AppendChild(xmiModel);

            var xmiExtension = doc.CreateElement("xmi", "Extension", xmiUri);
            xmiExtension.SetAttribute("extender", "Enterprise Architect");
            xmiExtension.SetAttribute("extenderID", "6.5");
            xmIElem.AppendChild(xmiExtension);

            var elements = doc.CreateElement( "elements");
            xmiExtension.AppendChild(elements);
            var packageElem = doc.CreateElement( "element");
            // packageElem.SetAttribute("xmi:Type", "uml:Package");
            packageElem.SetAttribute("Type", xmiUri, "uml:Package");
            packageElem.SetAttribute("name", "SAVETEST");
            packageElem.SetAttribute("scope", "public");
            elements.AppendChild(packageElem);
            foreach (var xmiClass in DiagramPool.Instance.ClassDiagram.Classes.Select(x => x.XMIParsedClass))
            {
                var element = doc.CreateElement( "element");
                element.SetAttribute("type", xmiUri, xmiClass.Type);
                element.SetAttribute("name", xmiClass.Name);
                element.SetAttribute("idref", xmiUri, xmiClass.XmiId);
                elements.AppendChild(element);
                if (xmiClass.attributes == null)
                    continue;
                XmlElement attributes = null;
                if (xmiClass.attributes.Any())
                {
                    attributes = doc.CreateElement("attributes");
                    element.AppendChild(attributes);
                }
                foreach (var xmiClassAttribute in xmiClass.attributes)
                {
                    var attribute = doc.CreateElement("attribute");
                    attribute.SetAttribute("idref", xmiUri, xmiClassAttribute.Id);
                    attribute.SetAttribute("name", xmiClassAttribute.Name);
                    var properties = doc.CreateElement("properties");
                    properties.SetAttribute("type", xmiClassAttribute.Type);
                    attribute.AppendChild(properties);
                    attributes?.AppendChild(attribute);
                }

            }

            // var umlModel = doc.CreateElement(string.Empty, "uml:Model", string.Empty);
            // doc.AppendChild(umlModel);
            

            // var element2 = doc.CreateElement( string.Empty, "level1", string.Empty );
            // // element1.AppendChild( element2 );
            //
            // var element3 = doc.CreateElement( string.Empty, "level2", string.Empty );
            // var text1 = doc.CreateTextNode( "text" );
            // element3.AppendChild( text1 );
            // element2.AppendChild( element3 );
            //
            // var element4 = doc.CreateElement( string.Empty, "level2", string.Empty );
            // var text2 = doc.CreateTextNode( "other text" );
            // element4.AppendChild( text2 );
            // element2.AppendChild( element4 );

            doc.Save( "D:\\Programovanie\\Bakalárka\\DPAnimArch\\UnityProjectDP\\Assets\\Resources\\SAVETEST.xml" );
        }
        
    }
}
