namespace SmartDownloads.Core;

public static class FolderCatalog
{
    private static readonly DownloadCategory[] Order = Enum.GetValues<DownloadCategory>();

    private static readonly Dictionary<string, string[]> Names = new()
    {
        ["tr"] = ["01_Projeler_ve_Kod", "02_Oyunlar_ve_Modlar", "03_Programlar_ve_Kurulumlar", "04_Gorseller", "05_Videolar", "06_Muzikler", "07_3D_Modeller", "08_Belgeler_ve_Veriler", "09_Arsivler", "10_Incelenecek"],
        ["en"] = ["01_Projects_and_Code", "02_Games_and_Mods", "03_Apps_and_Installers", "04_Images", "05_Videos", "06_Music", "07_3D_Models", "08_Documents_and_Data", "09_Archives", "10_Needs_Review"],
        ["ru"] = ["01_Проекты_и_код", "02_Игры_и_моды", "03_Программы_и_установщики", "04_Изображения", "05_Видео", "06_Музыка", "07_3D-модели", "08_Документы_и_данные", "09_Архивы", "10_Требует_проверки"],
        ["hi"] = ["01_प्रोजेक्ट_और_कोड", "02_गेम_और_मॉड", "03_ऐप्स_और_इंस्टॉलर", "04_चित्र", "05_वीडियो", "06_संगीत", "07_3D_मॉडल", "08_दस्तावेज़_और_डेटा", "09_संग्रह", "10_समीक्षा_आवश्यक"]
    };

    public static IReadOnlyDictionary<DownloadCategory, string> ForLanguage(string language)
    {
        var selected = Names.TryGetValue(language, out var names) ? names : Names["en"];
        return Order.Select((category, index) => (category, name: selected[index])).ToDictionary(x => x.category, x => x.name);
    }

    public static bool IsManagedFolder(string name) => Names.Values.SelectMany(x => x).Contains(name, StringComparer.OrdinalIgnoreCase);
}
