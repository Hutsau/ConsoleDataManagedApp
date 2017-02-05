using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConsoleDataManagedApp
{
    class Program
    {
        static int CurrPage = 0;
        static Thread thread;
        static Task task;
        static string Login;

        static List<User> Users = new List<User>();
        static List<Record> Data = new List<Record>();

        static readonly string LgPassFile = @"lg-pass.csv";
        static string CurrDataFile;

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, e) => {
                Login = null;
                CurrDataFile = null;
                CurrPage = 0;

                thread.Abort();
                thread = new Thread(Start);
                thread.Start();

                e.Cancel = true;
            };

            try {
                var lines = File.ReadAllLines(LgPassFile, Encoding.Default).Skip(1);
                foreach (var x in lines) {
                    var lg_pass = x.Split(',');
                    Users.Add(new User {
                        Login = lg_pass[0],
                        Password = lg_pass[1],
                        IsAdmin = Convert.ToBoolean(int.Parse(lg_pass[2]))
                    });
                }
            }
            catch (FileNotFoundException) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($" Файл {LgPassFile} не найден\r\n Работа программы будет завершена\r\n ");
                Console.ReadKey(true);
                Environment.Exit(0);
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($" Файл {LgPassFile} поврежден либо содержит неверный формат данных\r\n Ошибка: {ex.Message}\r\n Работа программы будет завершена\r\n ");
                Console.ReadKey(true);
                Environment.Exit(0);
            }

            thread = new Thread(Start);
            thread.Start();
        }

        public static void Start() {
            task = new Task(new Action(PageHelper));
            task.RunSynchronously();
        }

        public static void PageHelper() {
            m1:
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;

            CurrDataFile = null;

            var lg = "";
            var pass = "";
            var pos = Console.CursorTop;
            var currPos = Console.CursorTop;

            switch (CurrPage) {
                case 0:
                    Console.WriteLine(" Вход в систему...\r\n\r\n   A - Войти как администратор\r\n   U - Войти как пользователь\r\n   C - Закрыть программу\r\n");

                    ConsoleKey key;
                    do {
                        ClearCurrentConsoleLine();
                        Console.Write(" > ");

                        key = Console.ReadKey(true).Key;
                    } while (key != ConsoleKey.A && key != ConsoleKey.U && key != ConsoleKey.C);

                    switch (key) {
                        case ConsoleKey.A: CurrPage = 1; goto m1;
                        case ConsoleKey.U: CurrPage = 3; goto m1;
                    }

                    break;
                case 1:
                    Console.WriteLine(" <--- ctrl+c");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(" Администратор (необходима авторизация)\r\n");

                    Console.ForegroundColor = ConsoleColor.White;

                    pos = Console.CursorTop;

                    Console.WriteLine(" Введите логин:");
                    Console.Write(" >" );

                    currPos = Console.CursorTop;
                    while (lg.Equals(string.Empty))
                        try {
                            Console.SetCursorPosition(0, currPos);
                            Console.Write(" > ");
                            lg = Console.ReadLine().Trim();
                        }
                        catch { }

                    Console.WriteLine("\r\n Введите пароль:");
                    Console.Write(" > ");

                    currPos = Console.CursorTop;
                    while (pass.Equals(string.Empty))
                        try {
                            Console.SetCursorPosition(0, currPos);
                            Console.Write(" > ");
                            pass = Console.ReadLine().Trim();
                        }
                        catch { }

                    while (!CheckAdmin(lg, pass)) {
                        lg = "";
                        pass = "";
                        ClearConsoleLines(pos, 7);

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(" Ошибка авторизации. Неверный логин или пароль.");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(" Введите логин:");
                        Console.Write(" > ");

                        currPos = Console.CursorTop;
                        while (lg.Equals(string.Empty))
                            try {
                                Console.SetCursorPosition(0, currPos);
                                Console.Write(" > ");
                                lg = Console.ReadLine().Trim();
                            }
                            catch { }

                        Console.WriteLine("\r\n Введите пароль:");
                        Console.Write(" > ");

                        currPos = Console.CursorTop;
                        while (pass.Equals(string.Empty))
                            try {
                                Console.SetCursorPosition(0, currPos);
                                Console.Write(" > ");
                                pass = Console.ReadLine().Trim();
                            }
                            catch { }
                    }

                    Login = lg;
                    CurrPage = 2;
                    goto m1;
                case 2:
                    Console.WriteLine(" <--- ctrl+c");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($" {Login} (Администратор)\r\n");

                    Console.ForegroundColor = ConsoleColor.White;

                    Console.WriteLine(" Список доступных команд:");
                    Console.WriteLine("   EditLogin новое_имя");
                    Console.WriteLine("   EditPassword новый_пароль");
                    Console.WriteLine("   CreateUser имя_пользователя пароль [/A]");
                    Console.WriteLine("   DeleteUser имя_пользователя");
                    Console.WriteLine("   CreateDataFile путь_создания_файла.csv [/O]");
                    Console.WriteLine("   LoadDataFile путь_к_файлу.csv");
                    Console.WriteLine("   CreateOrEditRecord ИД УНП ИП Наименование Юридический_адрес Дата_регистрации Номер_телефона");
                    Console.WriteLine("   DeleteRecord ИД");
                    Console.WriteLine("   FindRecord поле значение");
                    Console.WriteLine("   ClearWindow\r\n");

                    while (true) {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(" > ");
                        PerformAdminCommand(Console.ReadLine());
                    }
                case 3:
                    Console.WriteLine(" <--- ctrl+c");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(" Локальный пользователь (необходима авторизация)\r\n");

                    Console.ForegroundColor = ConsoleColor.White;
                    //Console.WriteLine(" ctrl+c - вернуться назад\r\n");

                    pos = Console.CursorTop;

                    Console.WriteLine(" Введите логин:");
                    Console.Write(" >" );

                    currPos = Console.CursorTop;
                    while (lg.Equals(string.Empty))
                        try {
                            Console.SetCursorPosition(0, currPos);
                            Console.Write(" > ");
                            lg = Console.ReadLine().Trim();
                        }
                        catch { }

                    Console.WriteLine("\r\n Введите пароль:");
                    Console.Write(" > ");

                    currPos = Console.CursorTop;
                    while (pass.Equals(string.Empty))
                        try {
                            Console.SetCursorPosition(0, currPos);
                            Console.Write(" > ");
                            pass = Console.ReadLine().Trim();
                        }
                        catch { }

                    while (!CheckUser(lg, pass)) {
                        lg = "";
                        pass = "";
                        ClearConsoleLines(pos, 7);

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(" Ошибка авторизации. Неверный логин или пароль.");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(" Введите логин:");
                        Console.Write(" > ");

                        currPos = Console.CursorTop;
                        while (lg.Equals(string.Empty))
                            try {
                                Console.SetCursorPosition(0, currPos);
                                Console.Write(" > ");
                                lg = Console.ReadLine().Trim();
                            }
                            catch { }

                        Console.WriteLine("\r\n Введите пароль:");
                        Console.Write(" > ");

                        currPos = Console.CursorTop;
                        while (pass.Equals(string.Empty))
                            try {
                                Console.SetCursorPosition(0, currPos);
                                Console.Write(" > ");
                                pass = Console.ReadLine().Trim();
                            }
                            catch { }
                    }

                    Login = lg;
                    CurrPage = 4;
                    goto m1;
                case 4:
                    Console.WriteLine(" <--- ctrl+c");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($" {Login} (Локальный пользователь)\r\n");

                    Console.ForegroundColor = ConsoleColor.White;

                    Console.WriteLine(" Список доступных команд:");
                    Console.WriteLine("   EditLogin новое_имя");
                    Console.WriteLine("   EditPassword новый_пароль");
                    Console.WriteLine("   LoadDataFile путь_к_файлу");
                    Console.WriteLine("   FindRecord поле значение");
                    Console.WriteLine("   ClearWindow\r\n");

                    while (true) {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(" > ");
                        PerformUserCommand(Console.ReadLine());
                    }
            }
        }

        public static void PerformAdminCommand(string command) {
            if (command == null || command.Equals(string.Empty)) {
                ClearConsoleLines(Console.CursorTop - 1, 2);
                return;
            }

            command = command.Trim();

            if (Regex.IsMatch(command, "^EditLogin", RegexOptions.IgnoreCase)) {
                var pattern = @"^EditLogin (\S+)$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    var newLogin = Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups[1].Value;
                    Users.Find(x => x.Login == Login).Login = newLogin;
                    Login = newLogin;
                    CurrDataFile = null;

                    SaveUsers();

                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.White;

                    Console.WriteLine(" <--- ctrl+c");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($" {Login} (Администратор)\r\n");

                    Console.ForegroundColor = ConsoleColor.White;

                    Console.WriteLine(" Список доступных команд:");
                    Console.WriteLine("   EditLogin новое_имя");
                    Console.WriteLine("   EditPassword новый_пароль");
                    Console.WriteLine("   CreateUser имя_пользователя пароль [/A]");
                    Console.WriteLine("   DeleteUser имя_пользователя");
                    Console.WriteLine("   CreateDataFile путь_создания_файла.csv [/O]");
                    Console.WriteLine("   LoadDataFile путь_к_файлу.csv");
                    Console.WriteLine("   CreateOrEditRecord ИД УНП ИП Наименование Юридический_адрес Дата_регистрации Номер_телефона");
                    Console.WriteLine("   DeleteRecord ИД");
                    Console.WriteLine("   FindRecord поле значение");
                    Console.WriteLine("   ClearWindow\r\n");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^EditPassword", RegexOptions.IgnoreCase)) {
                var pattern = @"^EditPassword (\S+)$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    Users.Find(x => x.Login == Login).Password = Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups[1].Value;

                    SaveUsers();

                    Console.WriteLine("   Пароль успешно изменен.\r\n");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^CreateUser", RegexOptions.IgnoreCase)) {
                var pattern = @"^CreateUser (\S+) (\S+)( /A)?$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    var par = Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups;

                    if (Users.Any(x => x.Login == par[1].Value)) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   Пользователь уже существует.\r\n");
                        return;
                    }

                    Users.Add(new User {
                        Login = par[1].Value,
                        Password = par[2].Value,
                        IsAdmin = par[3].Value != string.Empty ? true : false
                    });

                    SaveUsers();

                    Console.WriteLine("   Пользователь успешно создан.\r\n");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^DeleteUser", RegexOptions.IgnoreCase)) {
                var pattern = @"^DeleteUser (\S+)$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    var user = Users
                        .Find(x => x.Login == Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups[1].Value);

                    if (user == null) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   Пользователь не существует.\r\n");
                        return;
                    }

                    if (user.Login == Login) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   У вас недостаточно прав для выполнения этой операции.\r\n");
                        return;
                    }

                    Users.Remove(user);

                    SaveUsers();

                    Console.WriteLine("   Пользователь успешно удален.\r\n");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^CreateDataFile", RegexOptions.IgnoreCase)) {
                var pattern = @"^CreateDataFile (\S+.csv)( /O)?$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    var par = Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups;

                    SaveData(par[1].Value);
                    Console.WriteLine("   Файл успешно создан.\r\n");

                    if (par[2].Value != string.Empty) {
                        CurrDataFile = par[1].Value;

                        LoadData();
                    }
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^LoadDataFile", RegexOptions.IgnoreCase)) {
                var pattern = @"^LoadDataFile (\S+.csv)$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    var file = Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups[1].Value;

                    if (!File.Exists(file)) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   Файл не найден.\r\n");
                        return;
                    }

                    CurrDataFile = file;
                    LoadData();
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^CreateOrEditRecord", RegexOptions.IgnoreCase)) {
                var pattern = @"^CreateOrEditRecord (\d+) (\d+) (\S+) (\S+) (\S+) (\d\d?[-./\\]\d\d?[-./\\]\d\d(\d\d)?) ([-+ \d]+)$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    if (CurrDataFile == null) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   Файл с данными не выбран. Сперва необходимо вызвать команду LoadDataFile путь_к_файлу.csv.\r\n");
                        return;
                    }

                    var par = Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups;

                    var record = Data.Find(x => x.ID == par[1].Value);
                    if (record != null) {
                        record.UNP = par[2].Value;
                        record.IP = par[3].Value;
                        record.NAIM = par[4].Value;
                        record.ADDRESS = par[5].Value;
                        record.DATE_REG = par[6].Value;
                        record.PHONE_NUMBER = par[8].Value;
                    } else Data.Add(new Record {
                        ID = par[1].Value,
                        UNP = par[2].Value,
                        IP = par[3].Value,
                        NAIM = par[4].Value,
                        ADDRESS = par[5].Value,
                        DATE_REG = par[6].Value,
                        PHONE_NUMBER = par[8].Value
                    });

                    SaveData(CurrDataFile);

                    Console.WriteLine("   Данные успешно изменены.\r\n");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^DeleteRecord", RegexOptions.IgnoreCase)) {
                var pattern = @"^DeleteRecord (\d+)$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    if (CurrDataFile == null) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   Файл с данными не выбран. Сперва необходимо вызвать команду LoadDataFile путь_к_файлу.csv.\r\n");
                        return;
                    }

                    var record = Data
                        .Find(x => x.ID == Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups[1].Value);

                    if (record == null) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   Запись не существует.\r\n");
                        return;
                    }

                    Data.Remove(record);
                    SaveData(CurrDataFile);

                    Console.WriteLine("   Запись успешно удалена.\r\n");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^FindRecord", RegexOptions.IgnoreCase)){
                var pattern = @"^FindRecord (\S+) (\S+)$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase))
                {
                    if (CurrDataFile == null) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   Файл с данными не выбран. Сперва необходимо вызвать команду LoadDataFile путь_к_файлу.csv.\r\n");
                        return;
                    }

                    var par = Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups;
                    var record = new Record();
                    List<Record> FilterData;

                    switch (par[1].Value.ToUpper())
                    {
                        case nameof(record.ID): FilterData = Data.Where(x => x.ID == par[2].Value).ToList(); break;
                        case nameof(record.UNP): FilterData = Data.Where(x => x.UNP == par[2].Value).ToList(); break;
                        case nameof(record.IP): FilterData = Data.Where(x => x.IP == par[2].Value).ToList(); break;
                        case nameof(record.NAIM): FilterData = Data.Where(x => x.NAIM == par[2].Value).ToList(); break;
                        case nameof(record.ADDRESS): FilterData = Data.Where(x => x.ADDRESS == par[2].Value).ToList(); break;
                        case nameof(record.DATE_REG): FilterData = Data.Where(x => x.DATE_REG == par[2].Value).ToList(); break;
                        case nameof(record.PHONE_NUMBER): FilterData = Data.Where(x => x.PHONE_NUMBER == par[2].Value).ToList(); break;
                        default:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("   Поле с таким именем отсутствует.\r\n");
                            return;
                    }

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(@"
 ================================================================================================================
 |  ID  |    UNP    |     IP     |      NAIM      |            ADDRESS            |  DATE_REG  |  PHONE_NUMBER  |
 ================================================================================================================
");

                    foreach (var x in FilterData)
                    {
                        Console.CursorLeft = 1;
                        Console.Write($"|{x.ID}");

                        Console.CursorLeft = 8;
                        Console.Write($"|{x.UNP}");

                        Console.CursorLeft = 20;
                        Console.Write($"|{x.IP}");

                        Console.CursorLeft = 33;
                        Console.Write($"|{x.NAIM}");

                        Console.CursorLeft = 50;
                        Console.Write($"|{x.ADDRESS}");

                        Console.CursorLeft = 82;
                        Console.Write($"|{x.DATE_REG}");

                        Console.CursorLeft = 95;
                        Console.Write($"|{x.PHONE_NUMBER}");

                        Console.CursorLeft = 112;
                        Console.Write("|");

                        Console.WriteLine(@"
 ----------------------------------------------------------------------------------------------------------------
");

                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^ClearWindow", RegexOptions.IgnoreCase)) {
                var pattern = @"^ClearWindow$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.White;

                    CurrDataFile = null;
                    Console.WriteLine(" <--- ctrl+c");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($" {Login} (Администратор)\r\n");

                    Console.ForegroundColor = ConsoleColor.White;

                    Console.WriteLine(" Список доступных команд:");
                    Console.WriteLine("   EditLogin новое_имя");
                    Console.WriteLine("   EditPassword новый_пароль");
                    Console.WriteLine("   CreateUser имя_пользователя пароль [/A]");
                    Console.WriteLine("   DeleteUser имя_пользователя");
                    Console.WriteLine("   CreateDataFile путь_создания_файла.csv [/O]");
                    Console.WriteLine("   LoadDataFile путь_к_файлу.csv");
                    Console.WriteLine("   CreateOrEditRecord ИД УНП ИП Наименование Юридический_адрес Дата_регистрации Номер_телефона");
                    Console.WriteLine("   DeleteRecord ИД");
                    Console.WriteLine("   FindRecord поле значение");
                    Console.WriteLine("   ClearWindow\r\n");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("   Команда не распознана. Смотри список доступных команд.\r\n");
        }

        public static void PerformUserCommand(string command) {
            if (command == null || command.Equals(string.Empty)) {
                ClearConsoleLines(Console.CursorTop - 1, 2);
                return;
            }

            command = command.Trim();

            if (Regex.IsMatch(command, "^EditLogin", RegexOptions.IgnoreCase)) {
                var pattern = @"^EditLogin (\S+)$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    var newLogin = Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups[1].Value;
                    Users.Find(x => x.Login == Login).Login = newLogin;
                    Login = newLogin;

                    try {
                        var newLines = new List<string>();
                        newLines.Add("LOGIN,PASSWORD,ISADMIN");
                        foreach (var x in Users)
                            newLines.Add($"{x.Login},{x.Password},{Convert.ToInt32(x.IsAdmin)}");

                        File.WriteAllLines(LgPassFile, newLines);
                    }
                    catch (Exception ex) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($" Ошибка: {ex.Message}\r\n Работа программы будет завершена\r\n ");
                        Console.ReadKey(true);
                        Environment.Exit(0);
                    }

                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.White;

                    CurrDataFile = null;
                    Console.WriteLine(" <--- ctrl+c");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($" {Login} (Локальный пользователь)\r\n");

                    Console.ForegroundColor = ConsoleColor.White;

                    Console.WriteLine(" Список доступных команд:");
                    Console.WriteLine("   EditLogin новое_имя");
                    Console.WriteLine("   EditPassword новый_пароль");
                    Console.WriteLine("   LoadDataFile путь_к_файлу");
                    Console.WriteLine("   FindRecord поле значение");
                    Console.WriteLine("   ClearWindow\r\n");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^EditPassword", RegexOptions.IgnoreCase)) {
                var pattern = @"^EditPassword (\S+)$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    Users.Find(x => x.Login == Login).Password = Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups[1].Value;

                    SaveUsers();

                    Console.WriteLine("   Пароль успешно изменен.\r\n");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^LoadDataFile", RegexOptions.IgnoreCase)) {
                var pattern = @"^LoadDataFile (\S+.csv)$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    var file = Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups[1].Value;

                    if (!File.Exists(file)) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   Файл не найден.\r\n");
                        return;
                    }

                    CurrDataFile = file;
                    LoadData();
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^FindRecord", RegexOptions.IgnoreCase)) {
                var pattern = @"^FindRecord (\S+) (\S+)$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    if (CurrDataFile == null) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("   Файл с данными не выбран. Сперва необходимо вызвать команду LoadDataFile путь_к_файлу.csv.\r\n");
                        return;
                    }

                    var par = Regex.Match(command, pattern, RegexOptions.IgnoreCase).Groups;
                    var record = new Record();
                    List<Record> FilterData;

                    switch (par[1].Value.ToUpper())
                    {
                        case nameof(record.ID): FilterData = Data.Where(x => x.ID == par[2].Value).ToList(); break;
                        case nameof(record.UNP): FilterData = Data.Where(x => x.UNP == par[2].Value).ToList(); break;
                        case nameof(record.IP): FilterData = Data.Where(x => x.IP == par[2].Value).ToList(); break;
                        case nameof(record.NAIM): FilterData = Data.Where(x => x.NAIM == par[2].Value).ToList(); break;
                        case nameof(record.ADDRESS): FilterData = Data.Where(x => x.ADDRESS == par[2].Value).ToList(); break;
                        case nameof(record.DATE_REG): FilterData = Data.Where(x => x.DATE_REG == par[2].Value).ToList(); break;
                        case nameof(record.PHONE_NUMBER): FilterData = Data.Where(x => x.PHONE_NUMBER == par[2].Value).ToList(); break;
                        default:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("   Поле с таким именем отсутствует.\r\n");
                            return;
                    }

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(@"
 ================================================================================================================
 |  ID  |    UNP    |     IP     |      NAIM      |            ADDRESS            |  DATE_REG  |  PHONE_NUMBER  |
 ================================================================================================================
");

                    foreach (var x in FilterData)
                    {
                        Console.CursorLeft = 1;
                        Console.Write($"|{x.ID}");

                        Console.CursorLeft = 8;
                        Console.Write($"|{x.UNP}");

                        Console.CursorLeft = 20;
                        Console.Write($"|{x.IP}");

                        Console.CursorLeft = 33;
                        Console.Write($"|{x.NAIM}");

                        Console.CursorLeft = 50;
                        Console.Write($"|{x.ADDRESS}");

                        Console.CursorLeft = 82;
                        Console.Write($"|{x.DATE_REG}");

                        Console.CursorLeft = 95;
                        Console.Write($"|{x.PHONE_NUMBER}");

                        Console.CursorLeft = 112;
                        Console.Write("|");

                        Console.WriteLine(@"
 ----------------------------------------------------------------------------------------------------------------
");

                    }
                }
             else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            if (Regex.IsMatch(command, "^ClearWindow", RegexOptions.IgnoreCase)) {
                var pattern = @"^ClearWindow$";

                if (Regex.IsMatch(command, pattern, RegexOptions.IgnoreCase)) {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.White;

                    Console.WriteLine(" <--- ctrl+c");

                    CurrDataFile = null;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($" {Login} (Локальный пользователь)\r\n");

                    Console.ForegroundColor = ConsoleColor.White;

                    Console.WriteLine(" Список доступных команд:");
                    Console.WriteLine("   EditLogin новое_имя");
                    Console.WriteLine("   EditPassword новый_пароль");
                    Console.WriteLine("   LoadDataFile путь_к_файлу");
                    Console.WriteLine("   FindRecord поле значение");
                    Console.WriteLine("   ClearWindow\r\n");
                } else {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("   Ошибка формата. Смотри описание команд.\r\n");
                }

                return;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("   Команда не распознана. Смотри список доступных команд.\r\n");
        }

        public static void SaveUsers() {
            try {
                var newLines = new List<string>();
                newLines.Add("LOGIN,PASSWORD,ISADMIN");
                foreach (var x in Users)
                    newLines.Add($"{x.Login},{x.Password},{Convert.ToInt32(x.IsAdmin)}");

                File.WriteAllLines(LgPassFile, newLines, Encoding.Default);
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($" Ошибка: {ex.Message}\r\n Работа программы будет завершена\r\n ");
                Console.ReadKey(true);
                Environment.Exit(0);
            }
        }

        public static void SaveData(string DataFile) {
            try {
                var newLines = new List<string>();
                newLines.Add("ID,UNP,IP,NAIM,ADDRESS,DETE_REG,PHONE_NUMBER");
                foreach (var x in Data)
                    newLines.Add($"{x.ID},{x.UNP},{x.IP},{x.NAIM},{x.ADDRESS},{x.DATE_REG},{x.PHONE_NUMBER}");

                File.WriteAllLines(DataFile, newLines, Encoding.Default);
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($" Ошибка: {ex.Message}\r\n Работа программы будет завершена\r\n ");
                Console.ReadKey(true);
                Environment.Exit(0);
            }
        }

        public static void LoadData() {
            Data.Clear();

            try {
                var lines = File.ReadAllLines(CurrDataFile, Encoding.Default).Skip(1);
                foreach (var x in lines) {
                    var data = x.Split(',');
                    Data.Add(new Record {
                        ID = data[0],
                        UNP = data[1],
                        IP = data[2],
                        NAIM = data[3],
                        ADDRESS = data[4],
                        DATE_REG = data[5],
                        PHONE_NUMBER = data[6]
                    });
                }
            }
            catch (FileNotFoundException) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($" Файл {LgPassFile} не найден\r\n Работа программы будет завершена\r\n ");
                Console.ReadKey(true);
                Environment.Exit(0);
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($" Файл {LgPassFile} поврежден либо содержит неверный формат данных\r\n Ошибка: {ex.Message}\r\n Работа программы будет завершена\r\n ");
                Console.ReadKey(true);
                Environment.Exit(0);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"   Данные готовы к работе ({CurrDataFile})");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(@"
 ================================================================================================================
 |  ID  |    UNP    |     IP     |      NAIM      |            ADDRESS            |  DATE_REG  |  PHONE_NUMBER  |
 ================================================================================================================
");

            foreach (var x in Data) {
                Console.CursorLeft = 1;
                Console.Write($"|{x.ID}");

                Console.CursorLeft = 8;
                Console.Write($"|{x.UNP}");

                Console.CursorLeft = 20;
                Console.Write($"|{x.IP}");

                Console.CursorLeft = 33;
                Console.Write($"|{x.NAIM}");

                Console.CursorLeft = 50;
                Console.Write($"|{x.ADDRESS}");

                Console.CursorLeft = 82;
                Console.Write($"|{x.DATE_REG}");

                Console.CursorLeft = 95;
                Console.Write($"|{x.PHONE_NUMBER}");

                Console.CursorLeft = 112;
                Console.Write("|");

                Console.WriteLine(@"
 ----------------------------------------------------------------------------------------------------------------
");
            }
        }

        public static bool CheckAdmin(string lg, string pass) {
            return Users.Any(x => x.IsAdmin && x.Login == lg && x.Password == pass);
        }

        public static bool CheckUser(string lg, string pass) {
            return Users.Any(x => !x.IsAdmin && x.Login == lg && x.Password == pass);
        }

        public static void ClearCurrentConsoleLine() {
            var pos = Console.CursorTop;
            Console.SetCursorPosition(0, pos);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, pos);
        }

        public static void ClearConsoleLines(int pos, int count) {
            for (int i = pos; i < pos + count; i++) {
                Console.SetCursorPosition(0, i);
                Console.Write(new string(' ', Console.WindowWidth));
            }
            Console.SetCursorPosition(0, pos);
        }
    }
}
