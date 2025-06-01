using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using RezeptbuchAPI.Models;

[XmlRoot("recipe")]
public class Recipe
{
    [Key]
    [XmlElement("hash")]
    public string Hash { get; set; } = string.Empty;

    [XmlElement("title")]
    public string Title { get; set; } = string.Empty;

    [XmlElement("imageName")]
    public string ImageName { get; set; } = string.Empty;

    [XmlElement("description")]
    public string Description { get; set; } = string.Empty;

    [XmlElement("servings")]
    public int? Servings { get; set; }

    [XmlElement("cookingTime")]
    public int CookingTime { get; set; }

    [XmlArray("categories")]
    [XmlArrayItem("category")]
    public List<Category> Categories { get; set; } = new();

    [XmlArray("instructions")]
    [XmlArrayItem("instruction")]
    public List<Instruction> Instructions { get; set; } = new();

    [XmlIgnore]
    public string OwnerUuid { get; set; } = string.Empty;

    [XmlIgnore]
    public Image? Image { get; set; }
}