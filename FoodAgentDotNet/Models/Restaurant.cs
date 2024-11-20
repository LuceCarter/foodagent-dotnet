using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FoodAgentDotNet.Models;

[BsonIgnoreExtraElements]
// We don't care about Id here as it is never used in the application so this attribute is added to ignore it.
[BsonNoId]
public class Restaurant
{
    [BsonElement("name")]
    public required string Name { get; set; }

    [BsonElement("address")]
    public Address? Address { get; set; }

    [BsonElement("cuisine")]
    public required string Cuisine { get; set; }
}

[BsonIgnoreExtraElements]
[BsonNoId]
public class Address
{
    [BsonElement("building")]
    public required string Building { get; set; }

    [BsonElement("street")]
    public required string Street { get; set; }

    [BsonElement("zipcode")]
    public required string Zipcode { get; set; }

    [BsonElement("coord")]
    public required double[] Coord { get; set; } // Coordinates as an array of [longitude, latitude]
}
