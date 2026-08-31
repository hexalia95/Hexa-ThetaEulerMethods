using System;
using ScottPlot;
using Spectre.Console;
using ThetaEulerMethods;


//highly experimental, only implementation will see if this is a viable scheme for XSquared
public class XSquaredTestEquation : LinearTestEquation
{
    // dX/dt = lambda*(X^2), X(1) = -1/lambda
    // true solution: X(t) = -1/(lambda*t) 
    // note: X_n now refers to X(1 + n*timestep)

    static string? eulerType;
    static bool thetaBattle = false;

    static double thetaValue;                       //these will be used to create the iterative formula later on
    static double lambdaValue;
    static double timestepValue;
    static double timeStopPoint;

    static List<double> timeAxis = new List<double>();              //these lists will contain the sets of points that will be plotted to a graph 
    static List<double> approxPoints = new List<double>();          //this will store the values from the iterative process (X_n)
    static List<double> exactPoints = new List<double>();           //this will contain the exact values (e^(m*n*timestep))
    static List<double> errorValuesAbs = new List<double>();        //this contains the absolute errors between each pair
    static List<double> errorValuesRel = new List<double>();        //this contains the relative errors between each pair


    static int iterN = 0;                           //inital values for the iteration
    static double currentX = 1d;
    static double currentEXact = 1d;


    //THETA COMPARISON EXCLUSIVE
    static readonly List<double> thetaSpaced = [.. Generate.Consecutive(101, 0.01, 0)];
    static List<double> globalErrors = new(101);


    private static double a; 
    private static double b;

    protected static double customStart = 1d;
    protected static double customStartIndex = 1d;


	public static void XSquaredAnalysis()
     // very similar to LinearAnalysis with key differences; see LinearTestEqn for more comments
	{

        Console.WriteLine("Welcome to the X-Squared Test Equation Space");

        eulerType = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which scheme should be used?")                      
                    .AddChoices("Explicit", "Implicit", "Theta", "Error Compare"));           

        AnsiConsole.MarkupLine($"You picked [blue]{eulerType}[/]");             //f(X,t) is what dX/dt is equal to (mX^2 here) (m is lambda)

        // call for a custom theta if that was the chosen option, set the theta values otherwise
        switch (eulerType)
        {
            case "Explicit":
                thetaValue = 1d;
                break;

            case "Implicit":
                thetaValue = 0d;
                break;

            case "Theta":
                var thetaPrompt = new TextPrompt<double>("What will be the [blue]theta value[/] (use 0.5 for trapezium)?")    //validate a custom theta input, disallows picking exactly 0 or 1
                    .Validate(input =>
                    {
                        if (input <= 0 || input >= 1)
                        {
                            return ValidationResult.Error("[red]Theta must be between 0 and 1, exclusive![/]");
                        }

                        return ValidationResult.Success();
                    });

                thetaValue = AnsiConsole.Prompt(thetaPrompt);
                AnsiConsole.MarkupLine($"You chose a theta value of [blue]{thetaValue}[/]");

                break;


            case "Error Compare":
                //set a flag to perform the theta-error comparison experiement with the given variables instead
                thetaBattle = true;
                break;

            default:
                break;

        }
        //get the coefficient
        var lambdaPrompt = new TextPrompt<double>("What will be the [blue]co-efficient[/]? [green](dX/dt = mX^2)[/]")
            .Validate(input =>
            {
                if (input == 0)
                {
                    return ValidationResult.Error("[red]Co-efficient must be non-zero![/]"); 
                }

                return ValidationResult.Success();
            });

        lambdaValue = AnsiConsole.Prompt(lambdaPrompt);
        AnsiConsole.MarkupLine($"You chose a co-efficent of [blue]{lambdaValue}[/]");




        // get the starting time, closer to 0 can be open to more variance
        // explicit incurs a restriction on timestep
        var startTimePrompt = new TextPrompt<double>("When should the [blue]simulation[/] begin? [green](Time in seconds, positive))[/]")
            .Validate(input =>
            {
                if (input <= 0)
                {
                    return ValidationResult.Error("[red]Time must be non-zero positive![/]");
                }

                return ValidationResult.Success();
            });

        customStart = AnsiConsole.Prompt(startTimePrompt);
        AnsiConsole.MarkupLine($"You chose a start time of [blue]{customStart}[/]");


        // get the timestep value (density of approximated points)
        // X_n must lie in a certain range (between 0 and -1/(lambda*timestep)) for the sign of the true solution to be respected
        //finally found proper condition for stability in schemes of theta nonzero (timestep must simply not exceed the starting time divided by theta)
        var timestepPrompt = new TextPrompt<double>("What will be the [blue]time step[/]? [green](X_n approximates X(1 + (n * timestep)))[/]")
            .Validate(input =>
            {
                if (input <= 0)
                {
                    return ValidationResult.Error("[red]Timestep must be positive![/]");
                }

                if ((thetaBattle || thetaValue != 0d) && input >= (customStart / thetaValue))
                {
                    return ValidationResult.Error($"[red]Timestep must be small enough for stability! ({customStart / thetaValue})[/]");
                }

                return ValidationResult.Success();
            });

        timestepValue = AnsiConsole.Prompt(timestepPrompt);
        AnsiConsole.MarkupLine($"You chose a timestep of [blue]{timestepValue}[/]");



        // get the "stopping time", this is the amount of time divided by the timestep, to get the number of iterates that will be made
        // so the iteration is stopped once n*timestep exceeds timeStopPoint
        var stopPointPrompt = new TextPrompt<double>("When should the [blue]simulation[/] end? [green](Time in seconds))[/]")
            .Validate(input =>
            {
                if (input <= 1)
                {
                    return ValidationResult.Error("[red]Time must exceed one![/]");
                }

                if (input < timestepValue)
                {
                    return ValidationResult.Error($"[red]Timestep exceeds the stopping point![/]"); 
                }

                return ValidationResult.Success();
            });

        timeStopPoint = AnsiConsole.Prompt(stopPointPrompt);
        AnsiConsole.MarkupLine($"You chose a stop time of [blue]{timeStopPoint}[/]");


        //table to summarise values before creating the iteration
        //basic for now, can stylise it later
        var table = new Table()
            .BorderColor(Spectre.Console.Color.Orange1)
            .Border(TableBorder.DoubleEdge)
            .Title("[green bold]Summary[/]")
            .ShowRowSeparators();


        table.AddColumn("[bold]Variable[/]");
        table.AddColumn("[bold]Value[/]");

        table.AddRow("Theta", $"{thetaValue}");
        table.AddRow("Co-efficient", $"{lambdaValue}");
        table.AddRow("Timestep", $"{timestepValue}");
        table.AddRow("Start Time", $"{customStart}");
        table.AddRow("Stopping Time", $"{timeStopPoint}");


        Console.WriteLine();
        AnsiConsole.Write(table);



        Console.WriteLine("Press Enter to Begin!");
        Console.ReadLine();



        if (!thetaBattle)
        {
            XSquaredIteration();
        }
        else
        {
            AnsiConsole.MarkupLine("[green bold] Beginning theta comparison![/]");
            XSThetaBattle();
        }
    }


    private static void XSquaredIteration()
    {
        if (!thetaBattle)
        {
            AnsiConsole.MarkupLine($"[green bold]Beginning Approximation[/], Initial value X_0 = X({customStart}) = {(lambdaValue > 0 ? -1d/(lambdaValue*customStart) : 1d / (lambdaValue * customStart))}");
        }


        //major difference to linear here, we have a proper formula instead of a basic multiplier iteration
        //first, we initialize "a" and "b", they will keep expressions clean

        a = (1d - thetaValue) * (lambdaValue) * (timestepValue);
        b = (thetaValue) * (lambdaValue) * (timestepValue);

        //making the time expression easier to use; will need to be refreshed when iterN changes
        customStartIndex = customStart + (iterN * timestepValue);

        //clean up before the iteration begins (will set XSquared exclusive values here)
        ResetIterationXSquared();

        while (customStartIndex <= timeStopPoint)
        {

            //starts by adding the default values to the Lists
            //the order here adds current values to the lists, then increments the time and gets the new values
            timeAxis.Add(customStartIndex);
            approxPoints.Add(currentX);
            exactPoints.Add(currentEXact);
            errorValuesAbs.Add(Math.Abs(currentX - currentEXact));
            errorValuesRel.Add(Math.Abs(currentX - currentEXact) / Math.Abs(currentEXact));

            //test point, printing values (explicit with lambda = 1 and timestep = 1 should be a doubler)
            //Console.WriteLine($"at time {customStartIndex}, the approx value of -1 over {lambdaValue}t is {currentX} and the true value is {currentEXact}");

            iterN++; customStartIndex = customStart + (iterN * timestepValue);
            currentX = thetaValue == 1d ? ExplicitFormula(currentX) : OtherFormula(currentX);
            currentEXact = -1d / (lambdaValue * (customStartIndex));
        }

        //testpoint, print the global (maximum error)
        Console.WriteLine($"the global error was: {errorValuesAbs.Max()}, and the global relative error was: {errorValuesRel.Max()}");



        if (!thetaBattle)
        {
            AnsiConsole.MarkupLine("[green bold]DONE![/]");
            XSquaredResultPlots();
        }


    }


    //need 2 different formulas: the one for all other theta values is based on the quadratic formula
    private static double ExplicitFormula(double xN)
    {
        return xN + (b * Math.Pow(xN, 2d));
    }

    private static double OtherFormula(double xN)
    {
        double result;
        double determinant = 1d - (4d * a * (xN + (b * Math.Pow(xN,2d))));

        //if we end up outside the valid range, zero the value
        double rootDet = Math.Sqrt(determinant);

        if (rootDet != 0)
        {
            //the root we choose is based on lambda: positive means negative X values, and vice versa
            //or not
            result = (1d - rootDet) / (2d * a);
        }
        else
        {
            return result = 0d;
        }

        return result;
    }


    private static void ResetIterationXSquared()
    {
        //reset of all values(for the sake of the "error compare" option
        timeAxis.Clear();
        exactPoints.Clear();
        approxPoints.Clear();
        errorValuesAbs.Clear();
        errorValuesRel.Clear();
        iterN = 0;
        currentX = -1d/(lambdaValue * customStart);
        currentEXact = -1d / (lambdaValue * customStart);
        customStartIndex = 1d;
    }



    private static void XSquaredResultPlots()
    {

        //rewrite comments ugh
        //basic for now, can style later
        //signal XYs can handle thousands of points while giving me custom spacing
        ScottPlot.Plot xSquaredResults = new();
        var exactCurve = xSquaredResults.Add.SignalXY(timeAxis, exactPoints, ScottPlot.Color.FromHex("ff0000"));
        var approxCurve = xSquaredResults.Add.SignalXY(timeAxis, approxPoints, ScottPlot.Color.FromHex("00ff00"));

        exactCurve.LegendText = "Exact";
        approxCurve.LegendText = "Approx.";
        xSquaredResults.Legend.Alignment = Alignment.MiddleCenter;

        xSquaredResults.Axes.SetLimitsY(-1d / (lambdaValue * customStart), 0);
        xSquaredResults.Axes.SetLimitsX(0, timeStopPoint);

        xSquaredResults.XLabel("t");
        xSquaredResults.YLabel("X");
        xSquaredResults.Title($"xSTE {(thetaValue == 0.5 ? "Trapezium" : eulerType)} FD Scheme {((thetaValue != 0d & thetaValue != 0.5d & thetaValue != 1d) ? "(Theta = " + thetaValue + ")" : "")}in timesteps of {timestepValue}: dX/dt = {((lambdaValue == 1d) ? "" : lambdaValue)}X^2 | X({customStart}) = {(lambdaValue > 0 ? -1d / (lambdaValue * customStart) : 1d / (lambdaValue * customStart))}");
        //interpolated title displays either Explicit, Implicit, Trapezium, or Theta; if theta, the thetaValue is also given


        xSquaredResults.ShowLegend();



        //the error plot is separate for now (multiplot later?)
        ScottPlot.Plot xSquaredErrors = new();
        var relErrorCurve = xSquaredErrors.Add.SignalXY(timeAxis, errorValuesRel, ScottPlot.Color.FromHex("0000ff"));

        xSquaredErrors.XLabel("t");
        xSquaredErrors.YLabel("Relative Error");
        xSquaredErrors.Title($"Relative Error in xSTE {(thetaValue == 0.5 ? "Trapezium" : eulerType)} FD Scheme {((thetaValue != 0d & thetaValue != 0.5d & thetaValue != 1d) ? "(Theta = " + thetaValue + ")" : "")}in timesteps of {timestepValue}: dX/dt = {((lambdaValue == 1d) ? "" : lambdaValue)}X^2 | X({customStart}) = {(lambdaValue > 0 ? -1d / (lambdaValue * customStart) : 1d / (lambdaValue * customStart))}");

        xSquaredErrors.Axes.MarginsY(0);
        xSquaredErrors.Axes.AutoScaleY();
        xSquaredErrors.Axes.SetLimitsX(0, timeStopPoint);
        


        string finalPath = DetermineSaveLocation();

        //decided against prompting for custom image names for now
        string finalResultsPath = Path.Combine(finalPath, "xSquaredResultsPLOT.png");
        string finalErrorPath = Path.Combine(finalPath, "xSquaredErrorPLOT.png");


        xSquaredResults.SavePng(finalResultsPath, 1280, 720);
        xSquaredErrors.SavePng(finalErrorPath, 1280, 720);

    }


    private static void XSThetaBattle()
    {
        for (int i = 0; i <= 100; i++)
        {
            //errorValuesRel.Clear();

            thetaValue = i / 100d;
            XSquaredIteration();            //set each theta, then perform the full iteration


            //Console.WriteLine(errorValuesRel.Count());

            globalErrors.Add(errorValuesRel.Max());      // append the corresponding global error each time
        }

        //testpoint, have values been collected correctly?
        foreach (double value in thetaSpaced)
        {
            Console.WriteLine($"at theta = {value}, the global error was {globalErrors[thetaSpaced.IndexOf(value)]}");
        }

        string titlestring = $"Error Comparison in xSTE FD Schemes of differing theta values for equation (in timesteps of {timestepValue}): dX/dt = {((lambdaValue == 1d) ? "" : lambdaValue)}X^2 | X({customStart}) = {(lambdaValue > 0 ? -1d / (lambdaValue * customStart) : 1d / (lambdaValue * customStart))}";
        ThetaBattlePlots(titlestring);
    }



    public XSquaredTestEquation()
	{
	}
}
