using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace FinalCurrencyConverter
{
    //A user with a username and hashed password
    class User
    {
        public string Username { get; set; }
        public string HashedPassword { get; set; }
    }

    //Registration and Login reading on a CSV file
    class UserDatabase
    {
        private readonly string csvFile;

        public UserDatabase(string csvFilePath = "users.csv")
        {
            csvFile = csvFilePath;
            if (!File.Exists(csvFile))
            {
                File.WriteAllText(csvFile, "Username,HashedPassword\n");
            }
        }

        public void Register()
        {
            Console.WriteLine("Proceeding to account creation...");
            string username = GetUniqueUsername();
            string password = GetValidPassword(username);

            string hashedPassword = HashPassword(password);
            File.AppendAllText(csvFile, $"{username},{hashedPassword}\n");
            Console.WriteLine("Account created successfully, Welcome " + username + "!");
        }

        public bool Login(string username, string password)
        {
            string hashedPassword = HashPassword(password);
            return File.ReadLines(csvFile).Skip(1).Any(line => line.Split(',')[0] == username && line.Split(',')[1] == hashedPassword);
        }

        public bool UserExists(string username)
        {
            return File.ReadLines(csvFile).Skip(1).Any(line => line.Split(',')[0] == username);
        }

        private string HashPassword(string password)
        {
            byte[] data = Encoding.UTF8.GetBytes(password);
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private string GetUniqueUsername()
        {
            while (true)
            {
                Console.Write("Pick a Username of your choice: ");
                string username = Console.ReadLine();

                if (UserExists(username))
                {
                    Console.WriteLine("Username already exists.");
                    Console.WriteLine("Press 1 to log in, or 2 to choose a different username:");
                    string choice = Console.ReadLine();
                    if (choice == "1")
                    {
                        return null;
                    }
                    else if (choice == "2")
                    {
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("Invalid choice. Please try again.");
                        continue;
                    }
                }
                return username;
            }
        }

        private string GetValidPassword(string username)
        {
            while (true)
            {
                Console.Write("Now create a password: ");
                string password = Console.ReadLine();
                if (password == username)
                {
                    Console.WriteLine("Password cannot be the same as the username. Please choose a different password.");
                }
                else
                {
                    return password;
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, Welcome to CashIt! Currency Converter");
            Console.WriteLine("You need to Login or Create an account.");
            Console.WriteLine("To login press 1, to create an account press 2");
            string userChoice = Console.ReadLine();
            while (userChoice != "1" && userChoice != "2")
            {
                Console.WriteLine("Wrong input, kindly enter the right response");
                userChoice = Console.ReadLine();
            }
            UserDatabase userDatabase = new UserDatabase();
            if (userChoice == "1")
            {
                LoginUser(userDatabase);
            }
            else if (userChoice == "2")
            {
                userDatabase.Register();
                LoginUser(userDatabase);
            }
        }

        static void LoginUser(UserDatabase userDatabase)
        {
            bool loggedIn = false;
            while (!loggedIn)
            {
                Console.Write("Enter your Username: ");
                string username = Console.ReadLine();
                if (!userDatabase.UserExists(username))
                {
                    Console.WriteLine("Sorry, username does not exist. Create an account by pressing Y for Yes or N to end? (Y/N)");
                    string answer = Console.ReadLine();
                    if (answer.Equals("Y", StringComparison.OrdinalIgnoreCase))
                    {
                        userDatabase.Register();
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                Console.Write("Enter your Password: ");
                string password = Console.ReadLine();
                if (userDatabase.Login(username, password))
                {
                    Console.WriteLine("Login successful!");
                    loggedIn = true;
                    CurrencyConversion().Wait();
                }
                else
                {
                    Console.WriteLine("Incorrect username or password. Please try again.\n");
                }

                if (!loggedIn && !RetryLogin())
                {
                    break;
                }
            }
        }

        static bool RetryLogin()
        {
            Console.WriteLine("Would you like to try again? Press Y for Yes or N for No");
            string answer = Console.ReadLine();
            return answer.Equals("Y", StringComparison.OrdinalIgnoreCase);
        }

        static async Task CurrencyConversion()
        {
            Console.Write("Enter amount to convert: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal amount))
            {
                Console.WriteLine("Invalid amount. Please enter a valid number.");
                return;
            }

            Console.Write("Enter base currency code (e.g. USD): ");
            string baseCurrency = Console.ReadLine().ToUpper();

            Console.Write("Enter target currency code (e.g. EUR): ");
            string targetCurrency = Console.ReadLine().ToUpper();

            decimal exchangeRate = await GetExchangeRate(baseCurrency, targetCurrency);
            if (exchangeRate == 0)
            {
                Console.WriteLine("Failed to retrieve exchange rate.");
                return;
            }

            decimal convertedAmount = amount * exchangeRate;
            Console.WriteLine($"{amount} {baseCurrency} is equivalent to {convertedAmount} {targetCurrency}");
        }

        static async Task<decimal> GetExchangeRate(string baseCurrency, string targetCurrency)
        {
            string url = $"https://api.exchangerate-api.com/v4/latest/{baseCurrency}";
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(jsonString);
                    if (json["rates"]?[targetCurrency] != null)
                    {
                        return json["rates"][targetCurrency].Value<decimal>();
                    }
                }
                Console.WriteLine("Error retrieving exchange rate.");
                return 0;
            }
        }
    }
}