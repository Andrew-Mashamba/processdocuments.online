using System.Text.Json;

namespace ZimaFileService.Tools;

/// <summary>
/// Calculator Tools - Various calculators and unit converters
/// </summary>
public class CalculatorTools
{
    #region Percentage Calculator

    /// <summary>
    /// Calculate percentage (what is X% of Y, X is what % of Y, etc.)
    /// </summary>
    public Task<string> CalculatePercentageAsync(Dictionary<string, object> args)
    {
        var operation = GetString(args, "operation", "of"); // of, is_what_percent, percent_change, add, subtract
        var value1 = GetDouble(args, "value1");
        var value2 = GetDouble(args, "value2");

        double result = 0;
        string description = "";

        switch (operation.ToLower())
        {
            case "of":
                // What is X% of Y?
                result = (value1 / 100) * value2;
                description = $"{value1}% of {value2} = {result}";
                break;

            case "is_what_percent":
                // X is what percent of Y?
                result = (value1 / value2) * 100;
                description = $"{value1} is {result:F2}% of {value2}";
                break;

            case "percent_change":
                // Percent change from X to Y
                result = ((value2 - value1) / value1) * 100;
                description = $"Change from {value1} to {value2} is {result:F2}%";
                break;

            case "add":
                // Add X% to Y
                result = value2 + (value2 * value1 / 100);
                description = $"{value2} + {value1}% = {result}";
                break;

            case "subtract":
                // Subtract X% from Y
                result = value2 - (value2 * value1 / 100);
                description = $"{value2} - {value1}% = {result}";
                break;
        }

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            operation,
            value1,
            value2,
            result = Math.Round(result, 6),
            description
        }));
    }

    #endregion

    #region Age Calculator

    /// <summary>
    /// Calculate age from birth date
    /// </summary>
    public Task<string> CalculateAgeAsync(Dictionary<string, object> args)
    {
        var birthDateStr = GetString(args, "birth_date");
        var toDateStr = GetString(args, "to_date", null);

        var birthDate = DateTime.Parse(birthDateStr);
        var toDate = string.IsNullOrEmpty(toDateStr) ? DateTime.Today : DateTime.Parse(toDateStr);

        var years = toDate.Year - birthDate.Year;
        var months = toDate.Month - birthDate.Month;
        var days = toDate.Day - birthDate.Day;

        if (days < 0)
        {
            months--;
            days += DateTime.DaysInMonth(toDate.Year, toDate.Month == 1 ? 12 : toDate.Month - 1);
        }

        if (months < 0)
        {
            years--;
            months += 12;
        }

        var totalDays = (toDate - birthDate).Days;
        var totalWeeks = totalDays / 7;
        var totalMonths = years * 12 + months;

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            birth_date = birthDate.ToString("yyyy-MM-dd"),
            current_date = toDate.ToString("yyyy-MM-dd"),
            age = new {
                years,
                months,
                days
            },
            totals = new {
                total_years = Math.Round((double)totalDays / 365.25, 2),
                total_months = totalMonths,
                total_weeks = totalWeeks,
                total_days = totalDays
            },
            next_birthday = new {
                date = new DateTime(toDate.Year + (toDate > new DateTime(toDate.Year, birthDate.Month, birthDate.Day) ? 1 : 0), birthDate.Month, birthDate.Day).ToString("yyyy-MM-dd"),
                days_until = ((new DateTime(toDate.Year + (toDate > new DateTime(toDate.Year, birthDate.Month, birthDate.Day) ? 1 : 0), birthDate.Month, birthDate.Day)) - toDate).Days
            }
        }));
    }

    #endregion

    #region BMI Calculator

    /// <summary>
    /// Calculate Body Mass Index
    /// </summary>
    public Task<string> CalculateBmiAsync(Dictionary<string, object> args)
    {
        var weight = GetDouble(args, "weight");
        var height = GetDouble(args, "height");
        var unit = GetString(args, "unit", "metric"); // metric (kg/m) or imperial (lb/in)

        double weightKg, heightM;

        if (unit == "imperial")
        {
            weightKg = weight * 0.453592; // lb to kg
            heightM = height * 0.0254; // inches to meters
        }
        else
        {
            weightKg = weight;
            heightM = height > 3 ? height / 100 : height; // Handle cm or m input
        }

        var bmi = weightKg / (heightM * heightM);

        var category = bmi switch
        {
            < 18.5 => "Underweight",
            < 25 => "Normal weight",
            < 30 => "Overweight",
            < 35 => "Obesity Class I",
            < 40 => "Obesity Class II",
            _ => "Obesity Class III"
        };

        // Calculate healthy weight range
        var minHealthyWeight = 18.5 * heightM * heightM;
        var maxHealthyWeight = 24.9 * heightM * heightM;

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            bmi = Math.Round(bmi, 1),
            category,
            weight = new { value = weightKg, unit = "kg" },
            height = new { value = heightM, unit = "m" },
            healthy_weight_range = new {
                min = Math.Round(minHealthyWeight, 1),
                max = Math.Round(maxHealthyWeight, 1),
                unit = "kg"
            }
        }));
    }

    #endregion

    #region Loan Calculator

    /// <summary>
    /// Calculate loan payments and amortization
    /// </summary>
    public Task<string> CalculateLoanAsync(Dictionary<string, object> args)
    {
        var principal = GetDouble(args, "principal");
        var annualRate = GetDouble(args, "annual_rate") / 100;
        var years = GetInt(args, "years", 0);
        var months = GetInt(args, "months", 0) + years * 12;
        var showSchedule = GetBool(args, "show_schedule", false);

        if (months == 0) months = 12;

        var monthlyRate = annualRate / 12;

        // Monthly payment formula: P * (r(1+r)^n) / ((1+r)^n - 1)
        double monthlyPayment;
        if (monthlyRate == 0)
        {
            monthlyPayment = principal / months;
        }
        else
        {
            var compound = Math.Pow(1 + monthlyRate, months);
            monthlyPayment = principal * (monthlyRate * compound) / (compound - 1);
        }

        var totalPayment = monthlyPayment * months;
        var totalInterest = totalPayment - principal;

        var result = new Dictionary<string, object>
        {
            ["success"] = true,
            ["principal"] = principal,
            ["annual_rate_percent"] = annualRate * 100,
            ["term_months"] = months,
            ["monthly_payment"] = Math.Round(monthlyPayment, 2),
            ["total_payment"] = Math.Round(totalPayment, 2),
            ["total_interest"] = Math.Round(totalInterest, 2)
        };

        if (showSchedule)
        {
            var schedule = new List<object>();
            var balance = principal;

            for (int i = 1; i <= Math.Min(months, 60); i++) // Limit to 60 months
            {
                var interestPayment = balance * monthlyRate;
                var principalPayment = monthlyPayment - interestPayment;
                balance -= principalPayment;

                schedule.Add(new {
                    month = i,
                    payment = Math.Round(monthlyPayment, 2),
                    principal = Math.Round(principalPayment, 2),
                    interest = Math.Round(interestPayment, 2),
                    balance = Math.Round(Math.Max(0, balance), 2)
                });
            }

            result["schedule"] = schedule;
            if (months > 60)
                result["schedule_note"] = $"Showing first 60 of {months} months";
        }

        return Task.FromResult(JsonSerializer.Serialize(result));
    }

    #endregion

    #region Unit Converter

    /// <summary>
    /// Convert between different units
    /// </summary>
    public Task<string> ConvertUnitAsync(Dictionary<string, object> args)
    {
        var value = GetDouble(args, "value");
        var from = GetString(args, "from").ToLower();
        var to = GetString(args, "to").ToLower();

        // Define conversion rates to base units
        var conversions = new Dictionary<string, (string category, double toBase)>
        {
            // Length (base: meters)
            ["m"] = ("length", 1),
            ["meter"] = ("length", 1),
            ["meters"] = ("length", 1),
            ["km"] = ("length", 1000),
            ["kilometer"] = ("length", 1000),
            ["cm"] = ("length", 0.01),
            ["centimeter"] = ("length", 0.01),
            ["mm"] = ("length", 0.001),
            ["millimeter"] = ("length", 0.001),
            ["mi"] = ("length", 1609.344),
            ["mile"] = ("length", 1609.344),
            ["miles"] = ("length", 1609.344),
            ["yd"] = ("length", 0.9144),
            ["yard"] = ("length", 0.9144),
            ["ft"] = ("length", 0.3048),
            ["foot"] = ("length", 0.3048),
            ["feet"] = ("length", 0.3048),
            ["in"] = ("length", 0.0254),
            ["inch"] = ("length", 0.0254),
            ["inches"] = ("length", 0.0254),

            // Weight (base: grams)
            ["g"] = ("weight", 1),
            ["gram"] = ("weight", 1),
            ["grams"] = ("weight", 1),
            ["kg"] = ("weight", 1000),
            ["kilogram"] = ("weight", 1000),
            ["mg"] = ("weight", 0.001),
            ["milligram"] = ("weight", 0.001),
            ["lb"] = ("weight", 453.592),
            ["lbs"] = ("weight", 453.592),
            ["pound"] = ("weight", 453.592),
            ["oz"] = ("weight", 28.3495),
            ["ounce"] = ("weight", 28.3495),
            ["ton"] = ("weight", 907185),
            ["tonne"] = ("weight", 1000000),

            // Volume (base: liters)
            ["l"] = ("volume", 1),
            ["liter"] = ("volume", 1),
            ["liters"] = ("volume", 1),
            ["ml"] = ("volume", 0.001),
            ["milliliter"] = ("volume", 0.001),
            ["gal"] = ("volume", 3.78541),
            ["gallon"] = ("volume", 3.78541),
            ["qt"] = ("volume", 0.946353),
            ["quart"] = ("volume", 0.946353),
            ["pt"] = ("volume", 0.473176),
            ["pint"] = ("volume", 0.473176),
            ["cup"] = ("volume", 0.236588),
            ["floz"] = ("volume", 0.0295735),
            ["tbsp"] = ("volume", 0.0147868),
            ["tsp"] = ("volume", 0.00492892),

            // Area (base: square meters)
            ["sqm"] = ("area", 1),
            ["sqkm"] = ("area", 1000000),
            ["sqmi"] = ("area", 2589988),
            ["sqft"] = ("area", 0.092903),
            ["sqyd"] = ("area", 0.836127),
            ["acre"] = ("area", 4046.86),
            ["hectare"] = ("area", 10000),

            // Speed (base: m/s)
            ["mps"] = ("speed", 1),
            ["kph"] = ("speed", 0.277778),
            ["kmh"] = ("speed", 0.277778),
            ["mph"] = ("speed", 0.44704),
            ["knot"] = ("speed", 0.514444),
            ["fps"] = ("speed", 0.3048),

            // Time (base: seconds)
            ["s"] = ("time", 1),
            ["sec"] = ("time", 1),
            ["second"] = ("time", 1),
            ["ms"] = ("time", 0.001),
            ["millisecond"] = ("time", 0.001),
            ["min"] = ("time", 60),
            ["minute"] = ("time", 60),
            ["h"] = ("time", 3600),
            ["hr"] = ("time", 3600),
            ["hour"] = ("time", 3600),
            ["d"] = ("time", 86400),
            ["day"] = ("time", 86400),
            ["wk"] = ("time", 604800),
            ["week"] = ("time", 604800),
            ["mo"] = ("time", 2592000),
            ["month"] = ("time", 2592000),
            ["yr"] = ("time", 31536000),
            ["year"] = ("time", 31536000),

            // Data (base: bytes)
            ["b"] = ("data", 1),
            ["byte"] = ("data", 1),
            ["kb"] = ("data", 1024),
            ["kilobyte"] = ("data", 1024),
            ["mb"] = ("data", 1048576),
            ["megabyte"] = ("data", 1048576),
            ["gb"] = ("data", 1073741824),
            ["gigabyte"] = ("data", 1073741824),
            ["tb"] = ("data", 1099511627776),
            ["terabyte"] = ("data", 1099511627776),
            ["bit"] = ("data", 0.125),
            ["kbit"] = ("data", 128),
            ["mbit"] = ("data", 131072),
            ["gbit"] = ("data", 134217728),
        };

        if (!conversions.ContainsKey(from))
        {
            return Task.FromResult(JsonSerializer.Serialize(new {
                success = false,
                error = $"Unknown unit: {from}"
            }));
        }

        if (!conversions.ContainsKey(to))
        {
            return Task.FromResult(JsonSerializer.Serialize(new {
                success = false,
                error = $"Unknown unit: {to}"
            }));
        }

        var (fromCategory, fromRate) = conversions[from];
        var (toCategory, toRate) = conversions[to];

        if (fromCategory != toCategory)
        {
            return Task.FromResult(JsonSerializer.Serialize(new {
                success = false,
                error = $"Cannot convert between {fromCategory} and {toCategory}"
            }));
        }

        // Convert to base unit, then to target
        var baseValue = value * fromRate;
        var result = baseValue / toRate;

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            from_value = value,
            from_unit = from,
            to_value = Math.Round(result, 6),
            to_unit = to,
            category = fromCategory
        }));
    }

    /// <summary>
    /// Convert temperature between units
    /// </summary>
    public Task<string> ConvertTemperatureAsync(Dictionary<string, object> args)
    {
        var value = GetDouble(args, "value");
        var from = GetString(args, "from", "c").ToLower();
        var to = GetString(args, "to", null);

        // Convert to Celsius first
        double celsius = from switch
        {
            "c" or "celsius" => value,
            "f" or "fahrenheit" => (value - 32) * 5 / 9,
            "k" or "kelvin" => value - 273.15,
            _ => value
        };

        // Convert to all units if 'to' not specified
        var results = new Dictionary<string, double>
        {
            ["celsius"] = Math.Round(celsius, 2),
            ["fahrenheit"] = Math.Round(celsius * 9 / 5 + 32, 2),
            ["kelvin"] = Math.Round(celsius + 273.15, 2)
        };

        if (!string.IsNullOrEmpty(to))
        {
            var result = to.ToLower() switch
            {
                "c" or "celsius" => results["celsius"],
                "f" or "fahrenheit" => results["fahrenheit"],
                "k" or "kelvin" => results["kelvin"],
                _ => results["celsius"]
            };

            return Task.FromResult(JsonSerializer.Serialize(new {
                success = true,
                from_value = value,
                from_unit = from,
                to_value = result,
                to_unit = to
            }));
        }

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            input = new { value, unit = from },
            conversions = results
        }));
    }

    #endregion

    #region Date Calculator

    /// <summary>
    /// Calculate difference between dates or add/subtract from a date
    /// </summary>
    public Task<string> CalculateDateAsync(Dictionary<string, object> args)
    {
        var operation = GetString(args, "operation", "difference"); // difference, add, subtract
        var date1Str = GetString(args, "date1", null);
        var date2Str = GetString(args, "date2", null);
        var days = GetInt(args, "days", 0);
        var months = GetInt(args, "months", 0);
        var years = GetInt(args, "years", 0);

        var date1 = string.IsNullOrEmpty(date1Str) ? DateTime.Today : DateTime.Parse(date1Str);

        switch (operation.ToLower())
        {
            case "difference":
                var date2 = string.IsNullOrEmpty(date2Str) ? DateTime.Today : DateTime.Parse(date2Str);
                var diff = (date2 - date1).TotalDays;
                var diffWeeks = (int)diff / 7;
                var diffMonths = (date2.Year - date1.Year) * 12 + date2.Month - date1.Month;
                var diffYears = date2.Year - date1.Year;

                return Task.FromResult(JsonSerializer.Serialize(new {
                    success = true,
                    date1 = date1.ToString("yyyy-MM-dd"),
                    date2 = date2.ToString("yyyy-MM-dd"),
                    difference = new {
                        days = (int)diff,
                        weeks = diffWeeks,
                        months = diffMonths,
                        years = diffYears,
                        business_days = CountBusinessDays(date1, date2)
                    }
                }));

            case "add":
                var addedDate = date1.AddYears(years).AddMonths(months).AddDays(days);
                return Task.FromResult(JsonSerializer.Serialize(new {
                    success = true,
                    original_date = date1.ToString("yyyy-MM-dd"),
                    added = new { years, months, days },
                    result_date = addedDate.ToString("yyyy-MM-dd"),
                    day_of_week = addedDate.DayOfWeek.ToString()
                }));

            case "subtract":
                var subtractedDate = date1.AddYears(-years).AddMonths(-months).AddDays(-days);
                return Task.FromResult(JsonSerializer.Serialize(new {
                    success = true,
                    original_date = date1.ToString("yyyy-MM-dd"),
                    subtracted = new { years, months, days },
                    result_date = subtractedDate.ToString("yyyy-MM-dd"),
                    day_of_week = subtractedDate.DayOfWeek.ToString()
                }));

            default:
                return Task.FromResult(JsonSerializer.Serialize(new {
                    success = false,
                    error = $"Unknown operation: {operation}"
                }));
        }
    }

    private int CountBusinessDays(DateTime start, DateTime end)
    {
        if (start > end)
            (start, end) = (end, start);

        int count = 0;
        for (var date = start; date < end; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }
        return count;
    }

    #endregion

    #region Tip Calculator

    /// <summary>
    /// Calculate tip and split bill
    /// </summary>
    public Task<string> CalculateTipAsync(Dictionary<string, object> args)
    {
        var billAmount = GetDouble(args, "bill_amount");
        var tipPercent = GetDouble(args, "tip_percent", 15);
        var splitWays = GetInt(args, "split_ways", 1);
        var roundUp = GetBool(args, "round_up", false);

        var tipAmount = billAmount * (tipPercent / 100);
        var totalAmount = billAmount + tipAmount;

        if (roundUp)
        {
            totalAmount = Math.Ceiling(totalAmount);
            tipAmount = totalAmount - billAmount;
        }

        var perPerson = totalAmount / splitWays;

        // Suggest tip amounts
        var suggestions = new Dictionary<string, object>
        {
            ["15%"] = new { tip = Math.Round(billAmount * 0.15, 2), total = Math.Round(billAmount * 1.15, 2) },
            ["18%"] = new { tip = Math.Round(billAmount * 0.18, 2), total = Math.Round(billAmount * 1.18, 2) },
            ["20%"] = new { tip = Math.Round(billAmount * 0.20, 2), total = Math.Round(billAmount * 1.20, 2) },
            ["25%"] = new { tip = Math.Round(billAmount * 0.25, 2), total = Math.Round(billAmount * 1.25, 2) }
        };

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            bill_amount = billAmount,
            tip_percent = tipPercent,
            tip_amount = Math.Round(tipAmount, 2),
            total_amount = Math.Round(totalAmount, 2),
            split_ways = splitWays,
            per_person = Math.Round(perPerson, 2),
            suggestions
        }));
    }

    #endregion

    #region Discount Calculator

    /// <summary>
    /// Calculate discounts and final price
    /// </summary>
    public Task<string> CalculateDiscountAsync(Dictionary<string, object> args)
    {
        var originalPrice = GetDouble(args, "original_price");
        var discountPercent = GetDouble(args, "discount_percent", 0);
        var discountAmount = GetDouble(args, "discount_amount", 0);
        var taxPercent = GetDouble(args, "tax_percent", 0);

        double discount;
        if (discountPercent > 0)
        {
            discount = originalPrice * (discountPercent / 100);
        }
        else
        {
            discount = discountAmount;
            discountPercent = (discount / originalPrice) * 100;
        }

        var priceAfterDiscount = originalPrice - discount;
        var taxAmount = priceAfterDiscount * (taxPercent / 100);
        var finalPrice = priceAfterDiscount + taxAmount;

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            original_price = originalPrice,
            discount = new {
                percent = Math.Round(discountPercent, 2),
                amount = Math.Round(discount, 2)
            },
            price_after_discount = Math.Round(priceAfterDiscount, 2),
            tax = new {
                percent = taxPercent,
                amount = Math.Round(taxAmount, 2)
            },
            final_price = Math.Round(finalPrice, 2),
            total_savings = Math.Round(discount, 2)
        }));
    }

    #endregion

    #region Interest Calculator

    /// <summary>
    /// Calculate simple and compound interest
    /// </summary>
    public Task<string> CalculateInterestAsync(Dictionary<string, object> args)
    {
        var principal = GetDouble(args, "principal");
        var rate = GetDouble(args, "rate") / 100;
        var time = GetDouble(args, "time"); // years
        var compoundFrequency = GetString(args, "compound", "yearly"); // yearly, monthly, quarterly, daily, simple

        double amount, interest;

        if (compoundFrequency == "simple")
        {
            // Simple interest
            interest = principal * rate * time;
            amount = principal + interest;
        }
        else
        {
            // Compound interest
            var n = compoundFrequency switch
            {
                "daily" => 365,
                "monthly" => 12,
                "quarterly" => 4,
                _ => 1 // yearly
            };

            amount = principal * Math.Pow(1 + rate / n, n * time);
            interest = amount - principal;
        }

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            principal,
            rate_percent = rate * 100,
            time_years = time,
            compound_frequency = compoundFrequency,
            interest = Math.Round(interest, 2),
            final_amount = Math.Round(amount, 2)
        }));
    }

    #endregion

    #region Math Expression Calculator

    /// <summary>
    /// Evaluate a mathematical expression
    /// </summary>
    public Task<string> EvaluateExpressionAsync(Dictionary<string, object> args)
    {
        var expression = GetString(args, "expression");

        try
        {
            // Simple expression evaluator (supports +, -, *, /, ^, %, parentheses)
            var result = EvaluateSimpleExpression(expression);

            return Task.FromResult(JsonSerializer.Serialize(new {
                success = true,
                expression,
                result = Math.Round(result, 10)
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonSerializer.Serialize(new {
                success = false,
                expression,
                error = ex.Message
            }));
        }
    }

    private double EvaluateSimpleExpression(string expression)
    {
        expression = expression.Replace(" ", "").ToLower();

        // Handle constants
        expression = expression.Replace("pi", Math.PI.ToString());
        expression = expression.Replace("e", Math.E.ToString());

        // Handle functions
        expression = System.Text.RegularExpressions.Regex.Replace(expression, @"sqrt\(([^)]+)\)", m =>
            Math.Sqrt(EvaluateSimpleExpression(m.Groups[1].Value)).ToString());
        expression = System.Text.RegularExpressions.Regex.Replace(expression, @"sin\(([^)]+)\)", m =>
            Math.Sin(EvaluateSimpleExpression(m.Groups[1].Value)).ToString());
        expression = System.Text.RegularExpressions.Regex.Replace(expression, @"cos\(([^)]+)\)", m =>
            Math.Cos(EvaluateSimpleExpression(m.Groups[1].Value)).ToString());
        expression = System.Text.RegularExpressions.Regex.Replace(expression, @"tan\(([^)]+)\)", m =>
            Math.Tan(EvaluateSimpleExpression(m.Groups[1].Value)).ToString());
        expression = System.Text.RegularExpressions.Regex.Replace(expression, @"log\(([^)]+)\)", m =>
            Math.Log10(EvaluateSimpleExpression(m.Groups[1].Value)).ToString());
        expression = System.Text.RegularExpressions.Regex.Replace(expression, @"ln\(([^)]+)\)", m =>
            Math.Log(EvaluateSimpleExpression(m.Groups[1].Value)).ToString());
        expression = System.Text.RegularExpressions.Regex.Replace(expression, @"abs\(([^)]+)\)", m =>
            Math.Abs(EvaluateSimpleExpression(m.Groups[1].Value)).ToString());

        // Handle parentheses recursively
        while (expression.Contains("("))
        {
            var match = System.Text.RegularExpressions.Regex.Match(expression, @"\(([^()]+)\)");
            if (match.Success)
            {
                var inner = match.Groups[1].Value;
                var result = EvaluateSimpleExpression(inner);
                expression = expression.Replace(match.Value, result.ToString());
            }
            else break;
        }

        // Handle exponents
        while (expression.Contains("^"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(expression, @"(-?[\d.]+)\^(-?[\d.]+)");
            if (match.Success)
            {
                var left = double.Parse(match.Groups[1].Value);
                var right = double.Parse(match.Groups[2].Value);
                expression = expression.Replace(match.Value, Math.Pow(left, right).ToString());
            }
            else break;
        }

        // Use DataTable to evaluate the rest
        var table = new System.Data.DataTable();
        var result2 = Convert.ToDouble(table.Compute(expression, ""));
        return result2;
    }

    #endregion

    #region Statistics Calculator

    /// <summary>
    /// Calculate statistics from a set of numbers
    /// </summary>
    public Task<string> CalculateStatisticsAsync(Dictionary<string, object> args)
    {
        var numbers = GetDoubleArray(args, "numbers");

        if (numbers.Length == 0)
        {
            return Task.FromResult(JsonSerializer.Serialize(new {
                success = false,
                error = "No numbers provided"
            }));
        }

        var sorted = numbers.OrderBy(x => x).ToArray();
        var count = numbers.Length;
        var sum = numbers.Sum();
        var mean = sum / count;
        var min = sorted[0];
        var max = sorted[count - 1];
        var range = max - min;

        // Median
        var median = count % 2 == 0
            ? (sorted[count / 2 - 1] + sorted[count / 2]) / 2
            : sorted[count / 2];

        // Mode
        var mode = numbers.GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .First().Key;

        // Variance and Standard Deviation
        var variance = numbers.Average(n => Math.Pow(n - mean, 2));
        var stdDev = Math.Sqrt(variance);

        // Quartiles
        var q1 = GetPercentile(sorted, 25);
        var q3 = GetPercentile(sorted, 75);
        var iqr = q3 - q1;

        return Task.FromResult(JsonSerializer.Serialize(new {
            success = true,
            count,
            sum = Math.Round(sum, 6),
            mean = Math.Round(mean, 6),
            median = Math.Round(median, 6),
            mode,
            min,
            max,
            range = Math.Round(range, 6),
            variance = Math.Round(variance, 6),
            standard_deviation = Math.Round(stdDev, 6),
            quartiles = new {
                q1 = Math.Round(q1, 6),
                q2 = Math.Round(median, 6),
                q3 = Math.Round(q3, 6),
                iqr = Math.Round(iqr, 6)
            }
        }));
    }

    private double GetPercentile(double[] sorted, int percentile)
    {
        var index = (percentile / 100.0) * (sorted.Length - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper)
            return sorted[lower];
        return sorted[lower] + (index - lower) * (sorted[upper] - sorted[lower]);
    }

    #endregion

    #region Helper Methods

    private static string GetString(Dictionary<string, object> args, string key, string? defaultValue = null)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.String ? je.GetString() ?? defaultValue ?? "" : je.ToString();
            }
            return value?.ToString() ?? defaultValue ?? "";
        }
        return defaultValue ?? "";
    }

    private static double GetDouble(Dictionary<string, object> args, string key, double defaultValue = 0)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.Number ? je.GetDouble() : defaultValue;
            }
            if (double.TryParse(value?.ToString(), out var result))
                return result;
        }
        return defaultValue;
    }

    private static int GetInt(Dictionary<string, object> args, string key, int defaultValue = 0)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.Number ? je.GetInt32() : defaultValue;
            }
            if (int.TryParse(value?.ToString(), out var result))
                return result;
        }
        return defaultValue;
    }

    private static bool GetBool(Dictionary<string, object> args, string key, bool defaultValue = false)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
            {
                return je.ValueKind == JsonValueKind.True ||
                       (je.ValueKind == JsonValueKind.String && je.GetString()?.ToLower() == "true");
            }
            if (bool.TryParse(value?.ToString(), out var result))
                return result;
        }
        return defaultValue;
    }

    private static double[] GetDoubleArray(Dictionary<string, object> args, string key)
    {
        if (args.TryGetValue(key, out var value))
        {
            if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                return je.EnumerateArray().Select(e => e.GetDouble()).ToArray();
            }
            if (value is IEnumerable<object> list)
            {
                return list.Select(x => Convert.ToDouble(x)).ToArray();
            }
        }
        return Array.Empty<double>();
    }

    #endregion
}
