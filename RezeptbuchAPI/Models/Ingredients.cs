using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace RezeptbuchAPI.Models
{
    public class Ingredient
    {
        [Key]
        [XmlIgnore]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("amount")]
        public int Amount { get; set; }

        [XmlAttribute("unit")]
        public string Unit { get; set; }

        [XmlIgnore]
        public int InstructionId { get; set; }

        [XmlIgnore]
        public Instruction Instruction { get; set; }
    }
}