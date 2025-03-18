using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


namespace FinalCurrencyConverter
{
    class Program
    {
        // Step 1: Store user data in an in-memory database (for demonstration purposes)
        static Dictionary<string, string> userDatabase = new Dictionary<string, string>();
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

            if (userChoice == "1")
            {
                LoginUser();
            }
            else if (userChoice == "2")
            {
                RegisterUser();
                // Option to login after registration, Tell the user to log in.
                LoginUser();
            }
        }


        // Step 2: Registration with password hashing
        static void RegisterUser()
        {
            Console.WriteLine("Proceeding to account creation...");
            Console.Write("Pick a Username of your choice: ");
            string username = Console.ReadLine();

            Console.Write("Username saved, now create a password: ");
            string password = Console.ReadLine();

            string hashedPassword = HashPassword(password);

            if (!userDatabase.ContainsKey(username))
            {
                userDatabase.Add(username, hashedPassword);
                Console.WriteLine("Account created successfully, you can now login.");
                Console.WriteLine("Username: " + username);
            }
            else
            {
                Console.WriteLine("Username already exists. Please try logging in or choose a different username.");
            }
        }

        // Step 3: Login with password verification
        static void LoginUser()
        {
            bool loggedIn = false;
            while (!loggedIn)
            {
                Console.Write("Enter your Username: ");
                string username = Console.ReadLine();
                Console.Write("Enter your Password: ");
                string password = Console.ReadLine();

                // Check if the username exists
                if (userDatabase.ContainsKey(username))
                {
                    string hashedPassword = HashPassword(password);
                    // Check if the password is correct
                    if (userDatabase[username] == hashedPassword)
                    {
                        Console.WriteLine("Login successful!");
                        loggedIn = true;
                        // Proceed to currency conversion after successful login.
                        CurrencyConversion().Wait(); // Wait because CurrencyConversion is async.
                    }
                    else
                    {
                        Console.WriteLine("Incorrect password. Please try again.\n");
                    }
                }
                else
                {
                    Console.WriteLine("No account can be found with that username. Please try again or register an account.\n");
                }

                // here, I'm asking if the user wants to exit the login loop,or try again
                if (!loggedIn)
                {
                    Console.WriteLine("Would you like to try logging in again? (Y/N)");
                    string retry = Console.ReadLine();
                    if (retry.Equals("N", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }
                
            }
        }

        // Step 4: Currency conversion prompt
        static async Task CurrencyConversion()
        {
            Console.Write("Enter amount to convert: ");
            decimal amount = decimal.Parse(Console.ReadLine());

            Console.Write("Enter base currency code (e.g. USD): ");
            string baseCurrency = Console.ReadLine().ToUpper();

            Console.Write("Enter target currency code (e.g. EUR): ");
            string targetCurrency = Console.ReadLine().ToUpper();

            // Step 5: Get live exchange rate from an API
            decimal exchangeRate = await GetExchangeRate(baseCurrency, targetCurrency);

            decimal convertedAmount = amount * exchangeRate;
            Console.WriteLine($"{amount} {baseCurrency} is equivalent to {convertedAmount} {targetCurrency}");
        }

        // Live exchange rate API call
        static async Task<decimal> GetExchangeRate(string baseCurrency, string targetCurrency)
        {
            // Replace YOUR_API_KEY with your actual API key and adjust the URL as needed.
            string url = $"https://api.exchangerate-api.com/v4/latest/{baseCurrency}";

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(jsonString);
                    decimal rate = json["rates"][targetCurrency].Value<decimal>();
                    return rate;
                }
                else
                {
                    Console.WriteLine("Error retrieving exchange rate.");
                    return 0;
                }
            }
        }

        // Password hashing
        static string HashPassword(string password)
        {
            byte[] data = Encoding.UTF8.GetBytes(password);
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }
    }
}