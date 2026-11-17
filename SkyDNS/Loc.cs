using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Markup;

namespace SkyDNS;

public class Loc : INotifyPropertyChanged
{
    public static Loc I { get; } = new();
    public event PropertyChangedEventHandler? PropertyChanged;

    string _lang = "fa";
    public string Lang
    {
        get => _lang;
        set
        {
            _lang = value == "en" ? "en" : "fa";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRtl)));
        }
    }
    public bool IsRtl => _lang == "fa";

    public string this[string key] =>
        D[_lang].TryGetValue(key, out var v) ? v : D["en"].TryGetValue(key, out var e) ? e : key;

    public string F(string key, params object[] args) => string.Format(this[key], args);

    static readonly Dictionary<string, Dictionary<string, string>> D = new()
    {
        ["fa"] = new()
        {
            ["appName"] = "اسکای DNS",
            ["servers"] = "سرورهای DNS",
            ["search"] = "جستجو…",
            ["add"] = "افزودن DNS",
            ["settings"] = "تنظیمات",
            ["widget"] = "ویجت",
            ["hint"] = "روی هر DNS کلیک کنید تا درجا روی ویندوز ست شود.",
            ["current"] = "DNS انتخاب‌شده",
            ["connected"] = "متصل است",
            ["connectedOther"] = "متصل به DNS دیگر",
            ["notConnected"] = "متصل نیست",
            ["applying"] = "در حال اعمال روی ویندوز…",
            ["connDesc"] = "ترافیک DNS کارت {0} الان از این سرور عبور می‌کند.",
            ["otherDesc"] = "الان روی «{0}» هستید. برای تعویض، دکمه اتصال را بزنید.",
            ["offDesc"] = "ویندوز از DNS خودکار (DHCP) استفاده می‌کند. برای ست کردن، دکمه اتصال را بزنید.",
            ["applyDesc"] = "تنظیم DNS اصلی و ثانویه و پاک کردن کش…",
            ["connect"] = "اتصال",
            ["disconnect"] = "قطع اتصال",
            ["primary"] = "DNS اصلی",
            ["secondary"] = "DNS ثانویه",
            ["latency"] = "تأخیر پاسخ",
            ["test"] = "تست پینگ",
            ["testAll"] = "تست همه",
            ["testing"] = "در حال تست…",
            ["edit"] = "ویرایش",
            ["del"] = "حذف",
            ["good"] = "عالی",
            ["ok"] = "قابل قبول",
            ["slow"] = "کند",
            ["addTitle"] = "افزودن DNS",
            ["editTitle"] = "ویرایش DNS",
            ["name"] = "عنوان",
            ["color"] = "رنگ",
            ["icon"] = "آیکون",
            ["cancel"] = "انصراف",
            ["save"] = "ذخیره",
            ["saveHint"] = "بعد از ذخیره با یک کلیک اعمال می‌شود.",
            ["errName"] = "عنوان و DNS اصلی را وارد کنید.",
            ["errIp"] = "آدرس IP معتبر نیست.",
            ["confirmDel"] = "«{0}» حذف شود؟",
            ["adapterTitle"] = "کارت شبکه",
            ["adapterDesc"] = "DNS روی کدام کارت شبکه ست شود؟",
            ["autoAdapter"] = "خودکار",
            ["autoAdapterD"] = "کارتی که به اینترنت وصل است ({0})",
            ["all"] = "همه کارت‌ها",
            ["allD"] = "روی همه کارت‌های فعال اعمال شود",
            ["up"] = "متصل",
            ["down"] = "قطع",
            ["errNoAdapter"] = "هیچ کارت شبکه فعالی پیدا نشد.",
            ["general"] = "عمومی",
            ["language"] = "زبان",
            ["startup"] = "اجرا با روشن شدن ویندوز",
            ["startupD"] = "برنامه بعد از ورود، در System Tray باز می‌شود.",
            ["tray"] = "بستن پنجره = رفتن به Tray",
            ["trayD"] = "برنامه در پس‌زمینه می‌ماند.",
            ["flush"] = "پاک کردن کش DNS بعد از هر تعویض",
            ["flushD"] = "معادل ipconfig /flushdns",
            ["restore"] = "بازگشت به DNS خودکار هنگام خروج",
            ["pingLaunch"] = "تست پینگ هنگام اجرا",
            ["open"] = "باز کردن اسکای DNS",
            ["exit"] = "خروج",
            ["autoDns"] = "DNS خودکار",
            ["close"] = "بستن",
        },
        ["en"] = new()
        {
            ["appName"] = "SkyDNS",
            ["servers"] = "DNS servers",
            ["search"] = "Search…",
            ["add"] = "Add DNS",
            ["settings"] = "Settings",
            ["widget"] = "Widget",
            ["hint"] = "Click any DNS to apply it to Windows instantly.",
            ["current"] = "Selected DNS",
            ["connected"] = "Connected",
            ["connectedOther"] = "Using another DNS",
            ["notConnected"] = "Not connected",
            ["applying"] = "Applying to Windows…",
            ["connDesc"] = "DNS traffic on {0} now goes through this server.",
            ["otherDesc"] = "You are on “{0}”. Press connect to switch.",
            ["offDesc"] = "Windows is using automatic DNS (DHCP). Press connect to apply this server.",
            ["applyDesc"] = "Setting primary and secondary DNS, flushing cache…",
            ["connect"] = "Connect",
            ["disconnect"] = "Disconnect",
            ["primary"] = "Primary DNS",
            ["secondary"] = "Secondary DNS",
            ["latency"] = "Latency",
            ["test"] = "Test ping",
            ["testAll"] = "Test all",
            ["testing"] = "Testing…",
            ["edit"] = "Edit",
            ["del"] = "Delete",
            ["good"] = "Great",
            ["ok"] = "Fair",
            ["slow"] = "Slow",
            ["addTitle"] = "Add DNS",
            ["editTitle"] = "Edit DNS",
            ["name"] = "Title",
            ["color"] = "Color",
            ["icon"] = "Icon",
            ["cancel"] = "Cancel",
            ["save"] = "Save",
            ["saveHint"] = "Once saved, it applies with one click.",
            ["errName"] = "Enter a title and a primary DNS.",
            ["errIp"] = "That IP address isn’t valid.",
            ["confirmDel"] = "Delete “{0}”?",
            ["adapterTitle"] = "Network adapter",
            ["adapterDesc"] = "Which adapter should DNS be set on?",
            ["autoAdapter"] = "Automatic",
            ["autoAdapterD"] = "The adapter connected to the internet ({0})",
            ["all"] = "All adapters",
            ["allD"] = "Apply to every active adapter",
            ["up"] = "Connected",
            ["down"] = "Disconnected",
            ["errNoAdapter"] = "No active network adapter found.",
            ["general"] = "General",
            ["language"] = "Language",
            ["startup"] = "Launch at Windows startup",
            ["startupD"] = "Opens in the system tray after sign-in.",
            ["tray"] = "Closing the window minimizes to tray",
            ["trayD"] = "The app keeps running in the background.",
            ["flush"] = "Flush DNS cache after every switch",
            ["flushD"] = "Same as ipconfig /flushdns",
            ["restore"] = "Restore automatic DNS on exit",
            ["pingLaunch"] = "Test ping on launch",
            ["open"] = "Open SkyDNS",
            ["exit"] = "Exit",
            ["autoDns"] = "Automatic DNS",
            ["close"] = "Close",
        },
    };
}

/// <summary>XAML: Text="{local:T key}" — live-updating localized string.</summary>
public class TExtension : MarkupExtension
{
    public TExtension(string key) { Key = key; }
    public string Key { get; set; }
    public override object ProvideValue(IServiceProvider sp) =>
        new Binding($"[{Key}]") { Source = Loc.I, Mode = BindingMode.OneWay }.ProvideValue(sp);
}
