using System.Collections.Generic;
using System.Xml.Serialization;

namespace RezeptbuchAPI.Models.DTO
{
    public class InstructionXmlDto
    {
        [XmlElement("ingredient", typeof(Ingredient))]
        [XmlText(typeof(string))]
        public List<object> Content { get; set; } = new();
    }
}