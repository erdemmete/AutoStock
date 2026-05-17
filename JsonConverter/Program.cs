using System.Text.Json;
using System.Text.Json.Serialization;

var cityMap = new Dictionary<string, string>
{
    ["1"] = "Adana",
    ["2"] = "Adıyaman",
    ["3"] = "Afyonkarahisar",
    ["4"] = "Ağrı",
    ["5"] = "Amasya",
    ["6"] = "Ankara",
    ["7"] = "Antalya",
    ["8"] = "Artvin",
    ["9"] = "Aydın",
    ["10"] = "Balıkesir",
    ["11"] = "Bilecik",
    ["12"] = "Bingöl",
    ["13"] = "Bitlis",
    ["14"] = "Bolu",
    ["15"] = "Burdur",
    ["16"] = "Bursa",
    ["17"] = "Çanakkale",
    ["18"] = "Çankırı",
    ["19"] = "Çorum",
    ["20"] = "Denizli",
    ["21"] = "Diyarbakır",
    ["22"] = "Edirne",
    ["23"] = "Elazığ",
    ["24"] = "Erzincan",
    ["25"] = "Erzurum",
    ["26"] = "Eskişehir",
    ["27"] = "Gaziantep",
    ["28"] = "Giresun",
    ["29"] = "Gümüşhane",
    ["30"] = "Hakkari",
    ["31"] = "Hatay",
    ["32"] = "Isparta",
    ["33"] = "Mersin",
    ["34"] = "İstanbul",
    ["35"] = "İzmir",
    ["36"] = "Kars",
    ["37"] = "Kastamonu",
    ["38"] = "Kayseri",
    ["39"] = "Kırklareli",
    ["40"] = "Kırşehir",
    ["41"] = "Kocaeli",
    ["42"] = "Konya",
    ["43"] = "Kütahya",
    ["44"] = "Malatya",
    ["45"] = "Manisa",
    ["46"] = "Kahramanmaraş",
    ["47"] = "Mardin",
    ["48"] = "Muğla",
    ["49"] = "Muş",
    ["50"] = "Nevşehir",
    ["51"] = "Niğde",
    ["52"] = "Ordu",
    ["53"] = "Rize",
    ["54"] = "Sakarya",
    ["55"] = "Samsun",
    ["56"] = "Siirt",
    ["57"] = "Sinop",
    ["58"] = "Sivas",
    ["59"] = "Tekirdağ",
    ["60"] = "Tokat",
    ["61"] = "Trabzon",
    ["62"] = "Tunceli",
    ["63"] = "Şanlıurfa",
    ["64"] = "Uşak",
    ["65"] = "Van",
    ["66"] = "Yozgat",
    ["67"] = "Zonguldak",
    ["68"] = "Aksaray",
    ["69"] = "Bayburt",
    ["70"] = "Karaman",
    ["71"] = "Kırıkkale",
    ["72"] = "Batman",
    ["73"] = "Şırnak",
    ["74"] = "Bartın",
    ["75"] = "Ardahan",
    ["76"] = "Iğdır",
    ["77"] = "Yalova",
    ["78"] = "Karabük",
    ["79"] = "Kilis",
    ["80"] = "Osmaniye",
    ["81"] = "Düzce"
};

var json = File.ReadAllText("ilce.json");

var exportItems = JsonSerializer.Deserialize<List<PhpMyAdminExportItem>>(json);

var districts = exportItems?
    .FirstOrDefault(x => x.Type == "table")?
    .Data ?? new List<DistrictItem>();

var result = districts
    .GroupBy(x => x.il_id)
    .Select(g => new
    {
        city = cityMap[g.Key],
        districts = g
            .Select(x => ToTitleCase(x.name))
            .Distinct()
            .OrderBy(x => x)
            .ToList()
    })
    .OrderBy(x => x.city)
    .ToList();

var output = JsonSerializer.Serialize(result, new JsonSerializerOptions
{
    WriteIndented = true
});

File.WriteAllText("turkey-locations.json", output);

Console.WriteLine("Bitti.");

static string ToTitleCase(string text)
{
    text = text.ToLower(new System.Globalization.CultureInfo("tr-TR"));

    return System.Globalization.CultureInfo
        .GetCultureInfo("tr-TR")
        .TextInfo
        .ToTitleCase(text);
}

public class PhpMyAdminExportItem
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("data")]
    public List<DistrictItem>? Data { get; set; }
}

public class DistrictItem
{
    public string id { get; set; } = "";
    public string il_id { get; set; } = "";
    public string name { get; set; } = "";
}