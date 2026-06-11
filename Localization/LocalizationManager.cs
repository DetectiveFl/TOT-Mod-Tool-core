using System.Reflection;
using OutlastTrialsMod.Config;
using OutlastTrialsMod.Mvvm;

namespace OutlastTrialsMod.Localization;

public sealed class LocalizationManager : ViewModelBase
{
    public static LocalizationManager Instance { get; } = new();

    private AppLanguage _currentLanguage = AppState.Language;

    private LocalizationManager()
    {
    }

    public AppLanguage CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (!SetProperty(ref _currentLanguage, value))
                return;

            AppState.Language = value;
            NotifyAllStrings();
        }
    }

    public void SetLanguage(AppLanguage language) => CurrentLanguage = language;

    public string GetString(string key) =>
        Strings.TryGetValue(key, out var translations) &&
        translations.TryGetValue(_currentLanguage, out var value)
            ? value
            : key;

    public string Format(string key, params object[] args) =>
        string.Format(GetString(key), args);

    public string AppTitle => GetString(nameof(AppTitle));
    public string ChangeDirectory => GetString(nameof(ChangeDirectory));
    public string Settings => GetString(nameof(Settings));
    public string BrowserTab => GetString(nameof(BrowserTab));
    public string ModTab => GetString(nameof(ModTab));
    public string Folders => GetString(nameof(Folders));
    public string Language => GetString(nameof(Language));
    public string English => GetString(nameof(English));
    public string Russian => GetString(nameof(Russian));
    public string Chinese => GetString(nameof(Chinese));
    public string SelectGameDirectoryTitle => GetString(nameof(SelectGameDirectoryTitle));
    public string SelectGameDirectoryHint => GetString(nameof(SelectGameDirectoryHint));
    public string Ok => GetString(nameof(Ok));
    public string Cancel => GetString(nameof(Cancel));
    public string Ready => GetString(nameof(Ready));
    public string SettingsPlaceholder => GetString(nameof(SettingsPlaceholder));
    public string CreateMod => GetString(nameof(CreateMod));
    public string Delete => GetString(nameof(Delete));
    public string SaveOriginalUasset => GetString(nameof(SaveOriginalUasset));
    public string ExportPngJson => GetString(nameof(ExportPngJson));
    public string Modify => GetString(nameof(Modify));
    public string Edit => GetString(nameof(Edit));
    public string Save => GetString(nameof(Save));
    public string NamespaceColumn => GetString(nameof(NamespaceColumn));
    public string KeyColumn => GetString(nameof(KeyColumn));
    public string ValueColumn => GetString(nameof(ValueColumn));
    public string LocresEditorTitle => GetString(nameof(LocresEditorTitle));
    public string LocresLoadFailed => GetString(nameof(LocresLoadFailed));
    public string LocresSaveFailed => GetString(nameof(LocresSaveFailed));
    public string LocresOpenFailed => GetString(nameof(LocresOpenFailed));
    public string SearchFilesPlaceholder => GetString(nameof(SearchFilesPlaceholder));
    public string EnterModName => GetString(nameof(EnterModName));
    public string ModNameEmpty => GetString(nameof(ModNameEmpty));
    public string CreatingMod => GetString(nameof(CreatingMod));
    public string ModBuildFailed => GetString(nameof(ModBuildFailed));
    public string ModBuildError => GetString(nameof(ModBuildError));
    public string PackingMod => GetString(nameof(PackingMod));
    public string DeleteConfirm => GetString(nameof(DeleteConfirm));
    public string PathNotFound => GetString(nameof(PathNotFound));
    public string Deleted => GetString(nameof(Deleted));
    public string DeleteFailed => GetString(nameof(DeleteFailed));
    public string ModifyPackagesOnly => GetString(nameof(ModifyPackagesOnly));
    public string ModifyTexture2DOnly => GetString(nameof(ModifyTexture2DOnly));
    public string ModifyDecodeFailed => GetString(nameof(ModifyDecodeFailed));
    public string TextureModifyError => GetString(nameof(TextureModifyError));
    public string SaveOriginalTitle => GetString(nameof(SaveOriginalTitle));
    public string ExportTitle => GetString(nameof(ExportTitle));
    public string Original => GetString(nameof(Original));
    public string NewModification => GetString(nameof(NewModification));
    public string Load => GetString(nameof(Load));
    public string Done => GetString(nameof(Done));
    public string TextureModifyTitle => GetString(nameof(TextureModifyTitle));
    public string TextureModifyDialogTitle => GetString(nameof(TextureModifyDialogTitle));
    public string SelectPngTexture => GetString(nameof(SelectPngTexture));
    public string LoadPngFailed => GetString(nameof(LoadPngFailed));
    public string LoadPngError => GetString(nameof(LoadPngError));
    public string LoadPngFirst => GetString(nameof(LoadPngFirst));
    public string SaveModifyFailed => GetString(nameof(SaveModifyFailed));
    public string GameDirectoryMissing => GetString(nameof(GameDirectoryMissing));
    public string PreparingBuild => GetString(nameof(PreparingBuild));
    public string InjectingTextures => GetString(nameof(InjectingTextures));
    public string CopyingForRepacker => GetString(nameof(CopyingForRepacker));
    public string FileError => GetString(nameof(FileError));
    public string ZeroFilesCopied => GetString(nameof(ZeroFilesCopied));
    public string RepackerNotFound => GetString(nameof(RepackerNotFound));
    public string SavingMod => GetString(nameof(SavingMod));
    public string RepackerNoPak => GetString(nameof(RepackerNoPak));
    public string ModBuildSuccess => GetString(nameof(ModBuildSuccess));
    public string Success => GetString(nameof(Success));
    public string RepackerStartFailed => GetString(nameof(RepackerStartFailed));
    public string RepackerExitCode => GetString(nameof(RepackerExitCode));
    public string PreparingFile => GetString(nameof(PreparingFile));
    public string InjectingFile => GetString(nameof(InjectingFile));
    public string TextureMetadataFailed => GetString(nameof(TextureMetadataFailed));

    private void NotifyAllStrings()
    {
        foreach (var property in typeof(LocalizationManager).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.PropertyType == typeof(string) && property.CanRead)
                OnPropertyChanged(property.Name);
        }
    }

    private static readonly Dictionary<string, Dictionary<AppLanguage, string>> Strings = new()
    {
        [nameof(AppTitle)] = Tr("Outlast Trials Mod Tool", "Outlast Trials Mod Tool", "逃生试炼：Mod工具"),
        [nameof(ChangeDirectory)] = Tr("Change Directory", "Сменить каталог", "更改路径"),
        [nameof(Settings)] = Tr("Settings", "Настройки", "设置"),
        [nameof(BrowserTab)] = Tr("Browser Tab (Game Directory)", "Обозреватель (каталог игры)", "游戏浏览"),
        [nameof(ModTab)] = Tr("Mod Tab (Modified Files)", "Моды (изменённые файлы)", "Mod目录"),
        [nameof(Folders)] = Tr("Folders", "Папки", "文件夹"),
        [nameof(Language)] = Tr("Language", "Язык", "语言"),
        [nameof(English)] = Tr("English", "English", "英语"),
        [nameof(Russian)] = Tr("Russian", "Русский", "俄语"),
        [nameof(Chinese)] = Tr("Chinese", "Китайский", "中文"),
        [nameof(SelectGameDirectoryTitle)] = Tr("Select Game Directory", "Выбор каталога игры", "选择游戏目录"),
        [nameof(SelectGameDirectoryHint)] = Tr(
            "Outlast Trials — game folder (contains .pak / .ucas / .utoc)",
            "Outlast Trials — папка игры (содержит .pak / .ucas / .utoc)",
            "逃生试炼 — 游戏文件夹 (包含 .pak / .ucas / .utoc)"),
        [nameof(Ok)] = Tr("OK", "ОК", "ОК"),
        [nameof(Cancel)] = Tr("Cancel", "Отмена", "取消"),
        [nameof(Ready)] = Tr("Ready", "Готово", "准备"),
        [nameof(SettingsPlaceholder)] = Tr(
            "Settings will be implemented in a future version.",
            "Дополнительные настройки появятся в будущих версиях.",
            "新的功能将在未来推出."),
        [nameof(CreateMod)] = Tr("Create Mod", "Создать мод", "创造Mod"),
        [nameof(Delete)] = Tr("Delete", "Удалить", "删除"),
        [nameof(SaveOriginalUasset)] = Tr("Save Original (.uasset)", "Сохранить оригинал (.uasset)", "保存源文件 (.uasset)"),
        [nameof(ExportPngJson)] = Tr("Export (PNG/JSON)", "Экспортировать (PNG/JSON)", "导出 (PNG/JSON)"),
        [nameof(Modify)] = Tr("Modify", "Модифицировать", "修改"),
        [nameof(Edit)] = Tr("Edit", "Редактировать", "编辑"),
        [nameof(Save)] = Tr("Save", "Сохранить", "保存"),
        [nameof(NamespaceColumn)] = Tr("Namespace", "Пространство имён", "文本"),
        [nameof(KeyColumn)] = Tr("Key", "Ключ", "Key"),
        [nameof(ValueColumn)] = Tr("Value", "Значение", "Value"),
        [nameof(LocresEditorTitle)] = Tr("Localization Editor — {0}", "Редактор локализации — {0}", "文本编辑器 — {0}"),
        [nameof(LocresLoadFailed)] = Tr("Failed to load localization file.", "Не удалось загрузить файл локализации.", "加载文本文件失败."),
        [nameof(LocresSaveFailed)] = Tr("Failed to save localization file: {0}", "Не удалось сохранить файл локализации: {0}", "保存文本文件失败 {0}"),
        [nameof(LocresOpenFailed)] = Tr("Failed to open localization editor: {0}", "Не удалось открыть редактор локализации: {0}", "打开文本文件失败: {0}"),
        [nameof(SearchFilesPlaceholder)] = Tr("Search files...", "Поиск файлов...", "寻找文件..."),
        [nameof(EnterModName)] = Tr("Enter mod name:", "Введите название мода:", "输入Mod名字:"),
        [nameof(ModNameEmpty)] = Tr("Mod name cannot be empty.", "Название мода не может быть пустым.", "Mod名字不能是空的."),
        [nameof(CreatingMod)] = Tr("Creating mod...", "Создание мода...", "正在创造Mod..."),
        [nameof(ModBuildFailed)] = Tr("Mod build failed.", "Ошибка сборки мода.", "Mod构建失败."),
        [nameof(ModBuildError)] = Tr("Build error: {0}", "Ошибка сборки: {0}", "构建错误: {0}"),
        [nameof(PackingMod)] = Tr("Packing mod...", "Упаковка мода...", "正在打包Mod..."),
        [nameof(DeleteConfirm)] = Tr("Are you sure you want to delete this?", "Вы уверены, что хотите удалить это?", "你确定要删除这些东西吗？"),
        [nameof(PathNotFound)] = Tr("Path not found.", "Путь не найден.", "路径未找到."),
        [nameof(Deleted)] = Tr("Deleted.", "Удалено.", "删除."),
        [nameof(DeleteFailed)] = Tr("Failed to delete: {0}", "Не удалось удалить: {0}", "删除错误: {0}"),
        [nameof(ModifyPackagesOnly)] = Tr(
            "Modification is only available for packages (.uasset / .umap).",
            "Модификация доступна только для пакетов (.uasset / .umap).",
            "修改仅适用于包(.uasset / .umap)."),
        [nameof(ModifyTexture2DOnly)] = Tr(
            "Modification is only available for Texture2D.",
            "Модификация доступна только для Texture2D.",
            "修改仅适用于 Texture2D."),
        [nameof(ModifyDecodeFailed)] = Tr(
            "Failed to decode the original texture.",
            "Не удалось декодировать оригинальную текстуру.",
            "解压原始贴图失败."),
        [nameof(TextureModifyError)] = Tr("Texture modification error.", "Ошибка модификации текстуры.", "贴图修改错误."),
        [nameof(SaveOriginalTitle)] = Tr("Save Original", "Сохранить оригинал", "保存源文件"),
        [nameof(ExportTitle)] = Tr("Export", "Экспортировать", "导出"),
        [nameof(Original)] = Tr("Original", "Оригинал", "原始"),
        [nameof(NewModification)] = Tr("New Modification", "Новая модификация", "新的修改"),
        [nameof(Load)] = Tr("Load", "Загрузить", "加载"),
        [nameof(Done)] = Tr("Done", "Готово", "完成"),
        [nameof(TextureModifyTitle)] = Tr("Texture Modification — {0}", "Модификация текстуры — {0}", "贴图修改 — {0}"),
        [nameof(TextureModifyDialogTitle)] = Tr("Texture Modification", "Модификация текстуры", "贴图修改"),
        [nameof(SelectPngTexture)] = Tr("Select PNG texture", "Выберите PNG-текстуру", "选择图片纹理"),
        [nameof(LoadPngFailed)] = Tr("Failed to load the selected PNG file.", "Не удалось загрузить выбранный PNG-файл.", "所选择的png加载失败."),
        [nameof(LoadPngError)] = Tr("Failed to load PNG: {0}", "Не удалось загрузить PNG: {0}", "png加载失败: {0}"),
        [nameof(LoadPngFirst)] = Tr(
            "Load a PNG first using the Load button.",
            "Сначала загрузите PNG с помощью кнопки «Загрузить».",
            "首先使用“加载”按钮加载PNG."),
        [nameof(SaveModifyFailed)] = Tr("Failed to save modification: {0}", "Не удалось сохранить модификацию: {0}", "保存修改错误: {0}"),
        [nameof(GameDirectoryMissing)] = Tr(
            "Game directory is not selected or does not exist. Select the game folder first.",
            "Папка игры не выбрана или не существует. Сначала укажите каталог игры.",
            "游戏目录未选择或不存在。首先选择游戏文件夹."),
        [nameof(PreparingBuild)] = Tr("Preparing build...", "Подготовка к сборке...", "准备构建..."),
        [nameof(InjectingTextures)] = Tr("Injecting textures...", "Инъекция текстур...", "注入纹理..."),
        [nameof(CopyingForRepacker)] = Tr("Copying files for repacker...", "Копирование файлов для упаковщика...", "复制文件以重新打包..."),
        [nameof(FileError)] = Tr("File error {0} — {1}", "Ошибка файла {0} — {1}", "文件错误 {0} — {1}"),
        [nameof(ZeroFilesCopied)] = Tr(
            "Copied 0 files! Searched in: {0}",
            "Скопировано 0 файлов! Программа искала их по пути: {0}",
            "已复制0个文件！已搜索: {0}"),
        [nameof(RepackerNotFound)] = Tr(
            "Repacker not found in Tools folder: {0}",
            "Упаковщик не найден в папке Tools: {0}",
            "在Tools文件夹中找不到Repacker: {0}"),
        [nameof(SavingMod)] = Tr("Saving mod...", "Сохранение готового мода...", "正在保存Mod..."),
        [nameof(RepackerNoPak)] = Tr("Repacker did not output a .pak file.", "Репакер не выдал .pak файл", "repacker未输出.pak文件"),
        [nameof(ModBuildSuccess)] = Tr(
            "Mod built and installed successfully!\nFile: {0}",
            "Мод успешно собран и установлен в игру!\nФайл: {0}",
            "已成功构建并安装Mod！\n文件: {0}"),
        [nameof(Success)] = Tr("Success", "Успех", "成功！"),
        [nameof(RepackerStartFailed)] = Tr("Failed to start repacker.", "Не удалось запустить упаковщик.", "无法打开repacker."),
        [nameof(RepackerExitCode)] = Tr(
            "Repacker exited with code {0}.",
            "Упаковщик завершился с кодом {0}.",
            "打包程序已退出 {0}."),
        [nameof(PreparingFile)] = Tr("Preparing: {0}", "Подготовка: {0}", "准备: {0}"),
        [nameof(InjectingFile)] = Tr("Injecting: {0}", "Инъекция: {0}", "注入: {0}"),
        [nameof(TextureMetadataFailed)] = Tr(
            "Failed to read UTexture2D metadata: {0}",
            "Не удалось прочитать метаданные UTexture2D: {0}",
            "读取UTexture2D元数据失败: {0}")
    };

    private static Dictionary<AppLanguage, string> Tr(string en, string ru, string cn) => new()
    {
        [AppLanguage.English] = en,
        [AppLanguage.Russian] = ru,
        [AppLanguage.Chinese] = cn
    };
}