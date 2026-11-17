# SkyDNS

برنامه ویندوزی برای ذخیره چند DNS با عنوان و ست کردن درجای آن‌ها روی ویندوز با یک کلیک.

ساخته‌شده با C# / WPF روی .NET 8 — رنگ اصلی `#0099FF` (Sky Blue) و رنگ دوم `#FDFDFE` (Cloudy White).

## قابلیت‌ها
- لیست DNSهای ذخیره‌شده با عنوان، رنگ و آیکون — کلیک روی هر کدام = ست شدن فوری روی ویندوز
- DNS اصلی و ثانویه، دکمه اتصال / قطع (بازگشت به DNS خودکار DHCP)
- تست تأخیر (کوئری واقعی DNS روی پورت ۵۳، در صورت شکست پینگ ICMP)
- انتخاب کارت شبکه: خودکار (کارت متصل به اینترنت)، یک کارت مشخص، یا همه کارت‌ها
- منوی System Tray برای سوییچ سریع و ویجت کوچک روی دسکتاپ
- اجرا با روشن شدن ویندوز (Task Scheduler با دسترسی ادمین، بدون پنجره UAC)
- پاک کردن خودکار کش DNS بعد از هر تعویض
- رابط فارسی (راست‌چین) و انگلیسی

## پیش‌نیاز
- Windows 10 یا 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) برای build

## اجرا و build
```bat
cd SkyDNS-Windows
dotnet run --project SkyDNS
```
یا ساخت فایل نصبی (بدون نیاز به نصب .NET روی سیستم مقصد). به [Inno Setup 6](https://jrsoftware.org/isdl.php) نیاز دارد:
```bat
build.bat
```
خروجی: `publish\SkyDNS-Setup.exe` (نصب‌کننده) و `publish\SkyDNS.exe`

### انتشار نسخه جدید
1. `<Version>` را در `SkyDNS\SkyDNS.csproj` بالا ببرید و `build.bat` را اجرا کنید.
2. در GitHub یک Release با تگ `vX.Y.Z` بسازید و `SkyDNS-Setup.exe` را با همین نام پیوست کنید؛ به‌روزرسان داخل برنامه همین فایل را دانلود و اجرا می‌کند.
3. `SkyDNS.exe` را هم پیوست کنید تا نسخه 1.2.0 (که خودش فایل exe را جایگزین می‌کند) هم به‌روز شود.

با Visual Studio 2022: فایل `SkyDNS.sln` را باز کنید. چون برنامه دسترسی ادمین می‌خواهد، Visual Studio را هم با **Run as administrator** باز کنید تا Debug کار کند.

## ساختار پروژه
```
SkyDNS/
  App.xaml(.cs)          استایل‌ها، رنگ‌ها، تک‌نمونه بودن برنامه، Tray
  MainWindow.xaml(.cs)   پنجره اصلی: لیست + جزئیات + دکمه اتصال
  EditWindow.xaml(.cs)   افزودن / ویرایش DNS
  SettingsWindow.xaml(.cs) تنظیمات
  WidgetWindow.xaml(.cs) ویجت کوچک
  MainViewModel.cs       منطق برنامه
  Models.cs              DnsEntry و AppSettings
  Services/DnsService.cs ست کردن DNS با netsh، پاک کردن کش، تشخیص کارت شبکه
  Services/PingService.cs تست تأخیر
  Services/StartupService.cs اجرا با ویندوز
  Services/TrayService.cs  آیکون و منوی System Tray
  Services/Storage.cs    ذخیره تنظیمات در %AppData%\SkyDNS\settings.json
  Loc.cs                 متن‌های فارسی و انگلیسی
```

## نکته‌ها
- DNS فقط برای IPv4 ست می‌شود. اگر شبکه شما IPv6 دارد، ویندوز ممکن است هنوز از DNS IPv6 روتر استفاده کند.
- برنامه از دستور `netsh interface ipv4 set dnsservers` استفاده می‌کند؛ همان کاری که از تنظیمات کارت شبکه دستی انجام می‌شود.
- آیکون برنامه در `SkyDNS/app.ico` است (همان طرح ابر آیکون Tray)؛ برای تغییر، همین فایل را جایگزین کنید.
