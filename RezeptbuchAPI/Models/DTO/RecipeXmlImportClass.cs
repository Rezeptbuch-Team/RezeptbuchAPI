using System.Collections.Generic;
using System.Xml.Serialization;

namespace RezeptbuchAPI.Models.DTO
{
    [XmlRoot("recipe")]
    public class RecipeXmlImport
    {
        [XmlElement("hash")]
        public string Hash { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("imageName")]
        public string ImageName { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("servings")]
        public int? Servings { get; set; }

        [XmlElement("cookingTime")]
        public int CookingTime { get; set; }

        [XmlArray("categories")]
        [XmlArrayItem("category")]
        public List<string> Categories { get; set; } = new();

        [XmlArray("instructions")]
        [XmlArrayItem("instruction")]
        public List<InstructionXmlDto> Instructions { get; set; } = new();
    }
}
