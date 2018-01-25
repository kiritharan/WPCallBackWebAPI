using System;
using System.Collections;
using System.Configuration;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MessengerBot.Controllers
{
    public class CardTypeInfo
    {
        public CardTypeInfo(string parameters)
        {
            string[] array = parameters.Split(",".ToCharArray());
            //CardTypeInfo(array[0], array[1], int.Parse(array[2]));
            Type = array[0];
            RegEx = array[1];
            Length = int.Parse(array[2]);

        }
        public CardTypeInfo(string type, string regEx, int length)
        {
            RegEx = regEx;
            Length = length;
            Type = type;
        }

        public string RegEx { get; set; }
        public int Length { get; set; }
        public string Type { get; set; }
    }
    class Validation
    {

        static string REGEXSTRING = string.Empty;

        static string PARATRING = string.Empty;

        static ArrayList REGEXARRAY = new ArrayList();

        static List<CardTypeInfo> _cardTypeInfo = null;

        //static string BADWORDKEYSTRING = string.Empty;

        public Validation(string regexString, ArrayList regexArray, List<CardTypeInfo> cardTypeInfo)//, String badWorkdKeyString)
        {
            REGEXSTRING = regexString;
            REGEXARRAY = regexArray;
            _cardTypeInfo = cardTypeInfo;
            // BADWORDKEYSTRING = badWorkdKeyString;
        }

        public List<CardTypeInfo> CardTypeInfo { get; set; }

        public string Detect(string data)
        {
            string detected = "";
            string cardInfo = string.Empty;
            string creditCardNumber = string.Empty;

            try
            {
                MatchCollection urlInData = Regex.Matches(data, @"(http|ftp|https)://([\w+?\.\w+])+([a-zA-Z0-9\~\!\@\#\$\%\^\&\*\(\)_\-\=\+\\\/\?\.\:\;\'\,]*)?");
                if (urlInData.Count > 0)
                {
                    foreach (Match match in urlInData)
                    {
                        data = data.Replace(match.Value.ToString(), "");
                    }
                }

                // Validate for credit card               
                Regex r = new Regex(REGEXARRAY[0].ToString());
                if (r.IsMatch(data))
                {
                    GroupCollection grpColl = r.Match(data).Groups;
                    foreach (Group g in grpColl)
                    {
                        if (!string.IsNullOrEmpty(g.Value))
                        {
                            creditCardNumber = g.Value.Trim();
                            creditCardNumber = Regex.Replace(creditCardNumber, "[^0-9]", "");
                            bool isItCreditCard = Mod10Check(creditCardNumber);

                            if (isItCreditCard)
                            {
                                string type = FindCardType(creditCardNumber);
                                cardInfo += "TYPE: " + type + " No: " + creditCardNumber;
                            }
                        }
                    }
                }
                if (cardInfo != "")
                    detected = "CREDIT CARD: " + cardInfo;


                // Validate for SIN
                if (detected == "")
                {
                    r = new Regex(REGEXARRAY[1].ToString());
                    string sinToValidate = string.Empty;

                    if (r.IsMatch(data))
                    {
                        foreach (Match match in r.Matches(data))
                        {
                            sinToValidate = match.Value.ToString().Replace(" ", string.Empty);
                            sinToValidate = sinToValidate.Replace("-", string.Empty);
                            if (validateSIN(sinToValidate))
                            { detected += " SIN Number found: " + match.Value; }
                        }
                    }
                }

                // Validate for SSN
                if (detected == "")
                {
                    r = new Regex(REGEXARRAY[2].ToString());
                    string matchedSSN = string.Empty;
                    if (r.IsMatch(data))
                    {
                        detected = "SSN Number Found";
                        foreach (Match match in r.Matches(data))
                        { matchedSSN += " " + match.Value; }
                        detected += "(" + matchedSSN + ")";
                    }
                }

                // All others
                if ((detected == "") && REGEXARRAY.Count > 3)
                {
                    string regexString = string.Empty;
                    for (int i = 3; i < REGEXARRAY.Count; i++)
                    {
                        regexString += REGEXARRAY[i];
                        if (REGEXARRAY.Count > i)
                            regexString += "|";

                        r = new Regex(regexString);
                        if (r.IsMatch(data))
                            detected = "OTHER Types Found";
                    }

                }
            }
            catch (Exception ex) { }

            return detected;
        }
        public bool Mod10Check(string creditCardNumber)
        {
            bool found = false;
            //// check whether input string is null or empty


            //// 1.	Starting with the check digit double the value of every other digit 
            //// 2.	If doubling of a number results in a two digits number, add up
            ///   the digits to get a single digit number. This will results in eight single digit numbers                    
            //// 3. Get the sum of the digits
            int sumOfDigits = creditCardNumber.Where((e) => e >= '0' && e <= '9')
                            .Reverse()
                            .Select((e, i) => ((int)e - 48) * (i % 2 == 0 ? 1 : 2))
                            .Sum((e) => e / 10 + e % 10);
            found = sumOfDigits % 10 == 0;



            //// If the final sum is divisible by 10, then the credit card number
            //   is valid. If it is not divisible by 10, the number is invalid.                   
            return found;
        }

        public bool validateSIN(string sinNumber)
        {
            int totalOfSINDigits = 0;
            int digitInEvenPosition = 0;
            int lastDigitInSIN = int.Parse(sinNumber.Substring((sinNumber.Length - 1), 1));

            for (int i = 0; i < (sinNumber.Length - 1); i++)
            {
                if (((i + 1) % 2) == 0)     //if even position number then double it and add it to totalOfSINDigits.
                {
                    digitInEvenPosition = int.Parse(sinNumber.Substring(i, 1)) * 2;

                    //if double of even position number is greater than 9, then subtract 9 from it to make single digit and then add it to totalOfSINDigits
                    totalOfSINDigits += (digitInEvenPosition < 10) ? digitInEvenPosition : digitInEvenPosition - 9;
                }
                else
                {
                    totalOfSINDigits += int.Parse(sinNumber.Substring(i, 1));
                }
            }

            if ((totalOfSINDigits % 10) == 0)
            {
                if (lastDigitInSIN == 0)
                { return true; }
                else return false;
            }
            else
            {
                if (lastDigitInSIN == 10 - ((totalOfSINDigits % 10) % 10))
                { return true; }
                else return false;
            }

        }

        private string FindCardType(string cardNumber)
        {
            string matchCard = string.Empty;
            foreach (CardTypeInfo info in _cardTypeInfo)
            {
                if (cardNumber.Length == info.Length && (Regex.IsMatch(cardNumber, info.RegEx)))
                    matchCard = info.Type;
            }

            return matchCard;
        }

        public string GetRegexString()
        {
            return REGEXSTRING;
        }

        public ArrayList GetRegexArray()
        {
            return REGEXARRAY;
        }

        public List<CardTypeInfo> GetCardType()
        { 
            return _cardTypeInfo;
        }
    }
}
