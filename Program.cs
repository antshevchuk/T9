// This is an independent project of an individual developer. Dear PVS-Studio, please check it.

// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com
using Microsoft.Data.SqlClient;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace T9
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Vocabulary vocabulary = new();
            Processor proc = new Processor();
            Nodes mode = Nodes.Process;
            ConsoleKeyInfo cki;
            Console.WriteLine(Processor.INFO);

           
            
            try
            {
                /////////////////////////////////////////////////////////////////////////
                ///Вход в режим процессора
                do
                {
                    mode = await proc.ProcessorMode(vocabulary, mode);
                }
                while (mode == Nodes.Process);
                /////////////////////////////////////////////////////////////////////////
                ///Вход в режим ввода
                if (mode == Nodes.Input)
                {
                    do
                    {
                        cki = Console.ReadKey(true);
                        mode = await proc.InputMode(vocabulary, cki, mode);
                    }
                    while (mode != Nodes.Exit);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Непредвиденая ошибка");
            }
        }
    }
}
/////////////////////////////////////////////////////////////////////////
///Объект по работе с базой данных
class DBT9
{
    public string SERVER = String.Empty;
    public string nameDB = String.Empty;
    public DBT9(string SERVER, string nameDB)
    {
        this.SERVER = SERVER;
        this.nameDB = nameDB;
    }
    /////////////////////////////////////////////////////////////////////////
    ///Создание базы данных
    public void CreateDB(SqlConnection connection, string nameDBt9)
    {
        SqlCommand command = new SqlCommand();
        command.CommandText = $"CREATE DATABASE {nameDBt9}";
        command.Connection = connection;
        command.ExecuteNonQuery();
        Console.WriteLine("База данных создана");
    }
    /////////////////////////////////////////////////////////////////////////
    ///Подключение
    public async Task<SqlConnection> ConnectDB()
    {
        string connectionString = $"Server=tcp:{SERVER}, 1433;Database={nameDB};Trusted_Connection=True;TrustServerCertificate=true;";
        SqlConnection connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }
    /////////////////////////////////////////////////////////////////////////
    ///Проверка доступности базы
    public async Task<bool> CheckDB()
    {
        try
        {
            await ConnectDB();
            return true;
        }
        catch (Exception)
        {
            Console.WriteLine("База данных не найдена либо недоступна.");
            return false;
        }
    }
    /////////////////////////////////////////////////////////////////////////
    ///Создание таблицы
    public void CreateTableDB(string nameTable, SqlConnection connection)
    {
        string sqlExpression = $"CREATE TABLE {nameTable} (Id INT PRIMARY KEY IDENTITY, Word VARCHAR(15) NOT NULL, Count INT NOT NULL)";
        SqlCommand command = new SqlCommand(sqlExpression, connection);
        command.ExecuteNonQuery();
        Console.WriteLine("Таблица Words создана");
    }
    /////////////////////////////////////////////////////////////////////////
    ///Первичное наполнение базы
    public void InsertToDBWord(SqlConnection connection, string word, int count)
    {
        string sqlExpression = $@"INSERT INTO [Words] ([Word], [Count]) VALUES (@word, @count)";
        SqlCommand command = new SqlCommand(sqlExpression, connection);
        command.Parameters.Add("@word", System.Data.SqlDbType.VarChar, 15).Value = word;
        command.Parameters.Add("@count", System.Data.SqlDbType.Int).Value = count;
        command.ExecuteNonQuery();
    }
    /////////////////////////////////////////////////////////////////////////
    ///Поиск
    public SqlDataReader FindWordInDB(SqlConnection connection, string word)
    {
        string sqlExpression = $@"SELECT Word, Count FROM [Words] WHERE Word=@word";
        SqlCommand command = new SqlCommand(sqlExpression, connection);
        command.Parameters.Add("@word", System.Data.SqlDbType.VarChar, 15).Value = word;
        SqlDataReader? reader = command.ExecuteReader();
        return reader;
    }
    /////////////////////////////////////////////////////////////////////////
    ///Обновление
    public void UpdateDB(SqlConnection connection, string word, int count)
    {
        string sqlExpression = $@"UPDATE Words SET Count=@count WHERE Word=@word";
        SqlCommand command = new SqlCommand(sqlExpression, connection);
        command.Parameters.Add("@word", System.Data.SqlDbType.VarChar, 15).Value = word;
        command.Parameters.Add("@count", System.Data.SqlDbType.Int).Value = count;
        command.ExecuteNonQuery();
    }
    /////////////////////////////////////////////////////////////////////////
    ///Очистка
    public void DeleteDB(SqlConnection connection)
    {
        string sqlExpression = $@"DELETE FROM Words";
        SqlCommand command = new SqlCommand(sqlExpression, connection);
        command.ExecuteNonQuery();
    }
    /////////////////////////////////////////////////////////////////////////
    ///Получение словаря значений по заданному параметру
    public async Task<Dictionary<string, int>> GetRelWordsFromDB(SqlConnection connection, string chunk)
    {
        string pattern = $@"^{chunk}\w*";
        Dictionary<string, int> dict = new Dictionary<string, int>();
        string sqlExpression = $@"SELECT Word, Count FROM [Words]";
        SqlCommand command = new SqlCommand(sqlExpression, connection);
        using (SqlDataReader? reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                if (Regex.IsMatch(reader[0].ToString(), pattern, RegexOptions.IgnoreCase))
                {
                    dict.Add(reader[0].ToString(), (int)reader[1]);
                }

            }
        };
        return dict;
    }

}
/////////////////////////////////////////////////////////////////////////
///Объект для работы с файлом
class WordsFile
{
    private string path = string.Empty;
    public string Path
    {
        get { return path; }
        set
        {
            if (File.Exists(value)) path = value;
            //else Console.WriteLine("File not exists");
        }
    }
    public WordsFile(string path)
    {
        Path = path;
    }
    /////////////////////////////////////////////////////////////////////////
    ///Проверка кодировки
    public bool CheckFile()
    {
        if (!String.IsNullOrEmpty(Path))
        {
            using (StreamReader reader = new StreamReader(path))
            {
                if (reader.CurrentEncoding == System.Text.Encoding.UTF8)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine("Неверная кодировка файла");
                    return false;
                }
            }
        }
        else
        {
            Console.WriteLine("Неверно указан путь к файлу");
            return false;
        }
    }
    /////////////////////////////////////////////////////////////////////////
    ///Чтение
    public List<string> ReadFile()
    {
        using (StreamReader reader = new StreamReader(path))
        {
            string? line = reader.ReadToEnd();
            char[] separators = new char[] { ' ', '.', ',', '\t', '\n' };
            string pattern = @"(\W+|\d+)";
            string target = "";
            Regex regex = new Regex(pattern);
            var wordsFromText = line.ToLower().Split(separators, StringSplitOptions.RemoveEmptyEntries).Select(x => regex.Replace(x, target)).Where(x => x.Length > 2 && x.Length < 16).ToList();
            return wordsFromText;
        }
    }
}
/////////////////////////////////////////////////////////////////////////
///Объект по работе со словарем
class Vocabulary
{
    public DBT9 _DBt9 { get; set; } = new DBT9("localhost", "T9db");
    public DBT9 _Master { get; set; } = new DBT9("localhost", "master");
    public Vocabulary() { }
    public Vocabulary(string SERVER)
    {
        _DBt9.SERVER = SERVER;
        _Master.SERVER = SERVER;
    }
    /////////////////////////////////////////////////////////////////////////
    ///Инициализация базаы данных и словаря
    public async Task<bool> CreateDataBase()
    {
        try
        {
            using (var connection = await _Master.ConnectDB())
            {
                _Master.CreateDB(connection, _DBt9.nameDB);
                Console.WriteLine("Подождите еще пару секунд. Создаются таблицы...");
                Thread.Sleep(6000);
            }
            using (var connection = await _DBt9.ConnectDB())
            {
                _DBt9.CreateTableDB("Words", connection);
            }
        }
        catch (SqlException)
        {
            Console.WriteLine("Низкая производительность системы...");
            Thread.Sleep(5000);
            using (var connection = await _DBt9.ConnectDB())
            {
                _DBt9.CreateTableDB("Words", connection);
            }
        }
        return true;
    }
    /////////////////////////////////////////////////////////////////////////
    ///Первичное наполнение словаря
    public async void CreateVocabulary(List<string> words)
    {
        if (words.Distinct().Count() != 0)
        {
            using (SqlConnection? connection = await _DBt9.ConnectDB())
            {
                foreach (var val in words.Distinct())
                {
                    int count = words.Where(x => x == val).Count();
                    if (count >= 3)
                    {
                        _DBt9.InsertToDBWord(connection, val, count - 2);
                    }
                }
            };
        }
        Console.WriteLine("Словарь создан");
    }
    /////////////////////////////////////////////////////////////////////////
    ///Обновление словаря
    public async void UpdateVocubalary(List<string> words)
    {
        if (words.Distinct().Count() != 0)
        {
            foreach (var val in words.Distinct())
            {
                using (SqlConnection? connection = await _DBt9.ConnectDB())
                {
                    int count = words.Where(x => x == val).Count();
                    if (count >= 3)
                    {
                        using (var reader = _DBt9.FindWordInDB(connection, val))
                        {
                            if (reader.HasRows)
                            {
                                using (SqlConnection con = await _DBt9.ConnectDB())
                                {
                                    while (reader.Read())
                                    {
                                        _DBt9.UpdateDB(con, val, (count + (int)reader[1] - 2));
                                    }
                                };
                            }
                            else
                            {
                                using (SqlConnection con = await _DBt9.ConnectDB())
                                {
                                    _DBt9.InsertToDBWord(con, val, count - 2);
                                };
                            }
                        };
                    }
                };
            }
        }
        Console.WriteLine("Словарь обновлен");
    }
    /////////////////////////////////////////////////////////////////////////
    ///Очитска словаря
    public async void DeleteVocabulary()
    {
        using (SqlConnection connection = await _DBt9.ConnectDB())
        {
            _DBt9.DeleteDB(connection);
        }
        Console.WriteLine("Словарь чист");
    }
    /////////////////////////////////////////////////////////////////////////
    ///Получаем список автодополнения
    public async void FindRelWordsInVocabulary(string chunk)
    {
        using (SqlConnection? connection = await _DBt9.ConnectDB())
        {
            var dict = await _DBt9.GetRelWordsFromDB(connection, chunk);
            var sortedDict = dict.OrderByDescending(v => v.Value).ThenBy(k => k.Key);
            for (int i = 0; i < sortedDict.Count() && i < 5; i++) Console.WriteLine(sortedDict.ElementAt(i).Key);
        }
    }
    /////////////////////////////////////////////////////////////////////////
}
/////////////////////////////////////////////////////////////////////////
///Текстовый процессор
class Processor
{
    public const string INFO = "///////////////////////////////////////////////////////////////////////////\n" +
                                "Welcome to T9!\n" +
                                "Использование:\n" +
                                "create [Disk:]\\[Path]\\[File]\n" +
                                "update [Disk:]\\[Path]\\[File]\n" +
                                "clear\n" +
                                "\n" +
                                "Здесь:\n" +
                                "[Disk:]\\[Path]\\[File] - расположение текстового файла в кодировке UTF-8\n" +
                                "create                  - создание словаря\n" +
                                "update                  - обновление словаря\n" +
                                "clear                   - очитска словаря\n" +
                                "///////////////////////////////////////////////////////////////////////////";
    /////////////////////////////////////////////////////////////////////////
    ///Режим процессора
    public async Task<Nodes> ProcessorMode(Vocabulary voc, Nodes mode)
    {
        var option = Console.ReadLine().Split(" ").ToList();
        if (!String.IsNullOrEmpty(option[0]))
        {
            switch (option[0].ToLower())
            {
                case "create":
                    {
                        //Create vocabulary
                        if (option.Count > 1)
                        {
                            string path = option[1].Trim('\"');
                            WordsFile WF = new WordsFile(path);
                            bool check = WF.CheckFile();
                            if (check)
                            {
                                var words = WF.ReadFile();
                                bool createDataBase = await voc.CreateDataBase();
                                if (createDataBase) voc.CreateVocabulary(words);
                                else Console.WriteLine("Что-то не так...");
                            }
                        }
                        else Console.WriteLine("Путь к файлу не указан");
                        break;
                    }
                case "update":
                    {
                        //Update vocabulary
                        if (option.Count > 1)
                        {
                            string path = option[1].Trim('\"');
                            WordsFile WF = new WordsFile(path);
                            bool checkFile = WF.CheckFile();
                            if (checkFile)
                            {
                                var words = WF.ReadFile();
                                bool checkDataBase = await voc._DBt9.CheckDB();
                                if (checkDataBase) voc.UpdateVocubalary(words);
                            }
                        }
                        else Console.WriteLine("Путь к файлу не указан");
                        break;
                    }
                case "clear":
                    {
                        //Clear vocabulary
                        bool checkDataBase = await voc._DBt9.CheckDB();
                        if (checkDataBase) voc.DeleteVocabulary();
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Введены неверные параметры.");
                        break;
                    }
            }
            return mode = Nodes.Process;
        }
        else
        {
            bool checkDataBase = await voc._DBt9.CheckDB();
            if (checkDataBase) return mode = Nodes.Input;
            else return mode = Nodes.Process;
        }
        //else return mode = Nodes.Input;
    }
    /////////////////////////////////////////////////////////////////////////
    ///Режим ввода
    public async Task<Nodes> InputMode(Vocabulary voc, ConsoleKeyInfo cki, Nodes mode)
    {
        switch (cki.Key)
        {
            case ConsoleKey.Escape:
                /*{
                    mode = Nodes.Exit;
                    break;
                }*/
            case ConsoleKey.Enter:
                {
                    mode = Nodes.Exit;
                    break;
                }
            default:
                {
                    Console.Write(">");
                    string? read = Console.ReadLine();
                    if (String.IsNullOrEmpty(read))
                    {
                        mode = Nodes.Exit;
                        break;
                    }
                    else
                    {
                        voc.FindRelWordsInVocabulary(read);
                        mode = Nodes.Process;
                        break;
                    }
                }
        }
        return mode;
    }

}

enum Nodes
{
    Process,
    Input,
    Exit
}