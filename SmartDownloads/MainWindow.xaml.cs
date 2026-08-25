using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SmartDownloads.Core;
using SmartDownloads.Services;

namespace SmartDownloads;

public partial class MainWindow : Window
{
    private string _language = "en";
    private DownloadOrganizer? _organizer;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private bool _exitRequested;
    private static readonly Dictionary<string, UiText> Texts = new()
    {
        ["tr"] = new("Program dilinizi seçin", "Program ve klasör adları seçtiğiniz dilde olacak.", "Programın dili ve oluşturulacak klasörlerin adları Türkçe olacak.", "Devam Et", "Düzenleyici hazırlanıyor...", "Bu işlem yalnızca kısa bir süre alacak.", "Dil ayarlanıyor", "Düzenli klasörler oluşturuluyor", "Otomatik düzenleme etkinleştiriliyor", "Kurulum tamamlanıyor", "Her şey hazır!", "İndirilenler klasörünüz artık otomatik olarak düzenli tutulacak.", "İndirilenleri Aç", "Bitir"),
        ["en"] = new("Choose your language", "The app and folder names will use your selected language.", "The application and folder names will be in English.", "Continue", "Preparing your organizer...", "This will only take a moment.", "Setting your language", "Creating organized folders", "Enabling automatic organization", "Finishing setup", "You're all set!", "Your Downloads folder will now stay organized automatically.", "Open Downloads", "Finish"),
        ["ru"] = new("Выберите язык программы", "Интерфейс и названия папок будут на выбранном языке.", "Интерфейс и названия папок будут на русском языке.", "Продолжить", "Подготавливаем органайзер...", "Это займёт совсем немного времени.", "Настраиваем язык", "Создаём удобные папки", "Включаем автоматическую сортировку", "Завершаем настройку", "Всё готово!", "Теперь папка «Загрузки» будет автоматически оставаться в порядке.", "Открыть загрузки", "Готово"),
        ["hi"] = new("ऐप की भाषा चुनें", "ऐप और फ़ोल्डर के नाम चुनी गई भाषा में होंगे।", "ऐप और बनाए गए फ़ोल्डर के नाम हिन्दी में होंगे।", "आगे बढ़ें", "व्यवस्थापक तैयार हो रहा है...", "इसमें केवल कुछ पल लगेंगे।", "भाषा सेट की जा रही है", "व्यवस्थित फ़ोल्डर बनाए जा रहे हैं", "स्वचालित व्यवस्था चालू की जा रही है", "सेटअप पूरा किया जा रहा है", "सब तैयार है!", "आपका डाउनलोड फ़ोल्डर अब अपने आप व्यवस्थित रहेगा।", "डाउनलोड खोलें", "समाप्त करें")
    };
    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;
        Closed += (_, _) => { _organizer?.Dispose(); _trayIcon?.Dispose(); };
        var saved = AppSettings.Load();
        if (saved is { SetupCompleted: true })
        {
            _language = saved.Language;
            SelectLanguageControl();
            ApplyText();
            LanguageView.Visibility = Visibility.Collapsed;
            CompletedView.Visibility = Visibility.Visible;
            StartOrganizer();
        }
        else SelectSystemLanguage();
    }
    private void SelectSystemLanguage()
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        _language = culture is "tr" or "ru" or "hi" ? culture : "en";
        SelectLanguageControl();
    }
    private void SelectLanguageControl()
    { if (_language == "tr") TurkishOption.IsChecked = true; else if (_language == "ru") RussianOption.IsChecked = true; else if (_language == "hi") HindiOption.IsChecked = true; else EnglishOption.IsChecked = true; }
    private void Language_Checked(object sender, RoutedEventArgs e)
    {
        _language = sender == TurkishOption ? "tr" : sender == RussianOption ? "ru" : sender == HindiOption ? "hi" : "en";
        ApplyText();
    }
    private void ApplyText()
    {
        var t = Texts[_language];
        WelcomeTitle.Text=t.Title; WelcomeSubtitle.Text=t.Subtitle; LanguageConfirmation.Text=t.Confirmation; ContinueButton.Content=t.Continue;
        PreparingTitle.Text=t.Preparing; PreparingSubtitle.Text=t.Wait; StepLanguage.Text="○ "+t.StepLanguage; StepFolders.Text="○ "+t.StepFolders; StepWatcher.Text="○ "+t.StepWatcher; StepReady.Text="○ "+t.StepReady;
        CompletedTitle.Text=t.Completed; CompletedMessage.Text=t.CompletedMessage; OpenDownloadsButton.Content=t.OpenDownloads; FinishButton.Content=t.Finish;
    }
    private async void Continue_Click(object sender, RoutedEventArgs e)
    {
        LanguageView.Visibility=Visibility.Collapsed; PreparingView.Visibility=Visibility.Visible;
        RingRotation.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0,360,TimeSpan.FromSeconds(1.2)){RepeatBehavior=RepeatBehavior.Forever});
        var t=Texts[_language]; await CompleteStep(StepLanguage,t.StepLanguage,420); new AppSettings(_language,true).Save(); await CompleteStep(StepFolders,t.StepFolders,520,CreateFolders); await CompleteStep(StepWatcher,t.StepWatcher,480,StartOrganizer); await CompleteStep(StepReady,t.StepReady,380);
        RingRotation.BeginAnimation(RotateTransform.AngleProperty,null); PreparingView.Visibility=Visibility.Collapsed; CompletedView.Visibility=Visibility.Visible;
    }
    private static async Task CompleteStep(TextBlock element,string text,int delay,Action? action=null)
    { element.Text="◌ "+text; element.Foreground=new SolidColorBrush(System.Windows.Media.Color.FromRgb(199,204,224)); await Task.Delay(delay); action?.Invoke(); element.Text="✓ "+text; element.Foreground=new SolidColorBrush(System.Windows.Media.Color.FromRgb(101,230,184)); }
    private void CreateFolders()
    { var downloads=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads"); foreach(var folder in FolderCatalog.ForLanguage(_language).Values) Directory.CreateDirectory(Path.Combine(downloads,folder)); }
    private void StartOrganizer()
    {
        if (_organizer is not null) return;
        var settings = AppSettings.Load() ?? new AppSettings(_language, true);
        _organizer = new DownloadOrganizer(settings); _organizer.Start();
        StartupRegistration.Enable();
        _trayIcon = new System.Windows.Forms.NotifyIcon { Icon = System.Drawing.SystemIcons.Application, Text = "Smart Downloads", Visible = true };
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Smart Downloads", null, (_, _) => Dispatcher.Invoke(ShowFromTray));
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(ExitApplication));
        _trayIcon.ContextMenuStrip = menu; _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowFromTray);
    }
    private void OpenDownloads_Click(object sender,RoutedEventArgs e)
    { var downloads=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads"); Process.Start(new ProcessStartInfo(downloads){UseShellExecute=true}); }
    private void Finish_Click(object sender,RoutedEventArgs e) => Hide();
    private void Close_Click(object sender,RoutedEventArgs e) { if (_organizer is null) { _exitRequested=true; Close(); } else Hide(); }
    private void ShowFromTray() { Show(); WindowState=WindowState.Normal; Activate(); }
    private void ExitApplication() { _exitRequested=true; Close(); }
    private void MainWindow_Closing(object? sender,System.ComponentModel.CancelEventArgs e) { if(_organizer is not null && !_exitRequested){e.Cancel=true;Hide();} }
    private void Window_MouseLeftButtonDown(object sender,MouseButtonEventArgs e){if(e.ButtonState==MouseButtonState.Pressed)DragMove();}
    private sealed record UiText(string Title,string Subtitle,string Confirmation,string Continue,string Preparing,string Wait,string StepLanguage,string StepFolders,string StepWatcher,string StepReady,string Completed,string CompletedMessage,string OpenDownloads,string Finish);
}
