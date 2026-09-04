using System;
using ScottPlot;
using Spectre.Console;
using ThetaEulerMethods;


public class XSquaredTestEquation : LinearTestEquation
{
    /*
    * Here is the class file for the X Squared test equation (xSTE)
    * For the most part, this is structured very similarly to the LTE class file, so the more detailed documentation will be in there.
    * This class inherits the LTE class for the sake of the DetermineSaveLocation() and ThetaBattlePlots() methods.
    * 
    * The true solution for the xSTE "dX/dt = λx^2, with X(1) = -1/λ" is X(t) = -1/λt
    * The approximation recurrence is given by either: X_n+1 = X_n + b * (X_n)^2 | in the Explicit scheme...
    * ...or by X_n+1 = 1 - sqrt[1 - (4 * a * (X_n + b * (X_n)^2))] / 2a | for any other theta value. 
    * a = (1-θ)λΔt and b = θλΔt
    * 
    * For this function, the user can input a start time (>0) as opposed to the LTE that always starts at 0. This has an impact on the valid range of Δt.
    * In comments, i will refer to this start time as "C".
    */

    //MAIN VARIABLES
    static string? eulerType;                       //Stores the chosen Euler option (will be used when titling the plots)
    static bool thetaBattle = false;                //Stores whether an Error Compare should be performed

    static double thetaValue;                       //Stores the theta: it must be inputted manually if Explicit (=1) or Implicit (=0) weren't chosen
    static double lambdaValue;                      //Stores the coefficient: must be nonzero
    static double timestepValue;                    //Stores Δt, will affect the number of iterations that need to be computed
    static double timeStopPoint;                    //Stores the chosen stop time (eg. after 10 seconds). Note that time based variables are always in seconds.

    static List<double> timeAxis = [];              //List of time points (our eventual x-axis)
    static List<double> approxPoints = [];          //Stores the values from the iterative process (X_n) (our y-axis)
    static List<double> exactPoints = [];           //Stores the exact values of the function e^(λnΔt)
    static List<double> errorValuesAbs = [];        //Stores the absolute errors between each pair (exact and approx values)
    static List<double> errorValuesRel = [];        //Stores the relative errors between each pair

    static int iterN = 0;                           //Indexer to control our iteration
    static double currentX = 1d;                    //currentX is X_n in our approximation
    static double currentEXact = 1d;                //currentEXact is the true value of -1/λt


    //ERROR COMPARE EXCLUSIVE
    static readonly List<double> thetaSpaced = [.. Generate.Consecutive(101, 0.01, 0)];  //Stores [0,0.01,0.02,...,0.99,1]
    static List<double> globalErrors = new(101);                                         //Stores the maximum relative errors for each simulation under the thetas given above



    private static double a;                        //Stores the value of (1-θ)λΔt which will be used for the iterative recurrence formula
    private static double b;                        //Stores the value of θλΔt for the same reason as above

    protected static double customStart = 1d;       //Stores the user-chosen starting point for the simulation
    protected static double customStartIndex = 1d;  //This stores the value of customStart + (iterN * Δt), and will be used to produce points in the simulation up to the stopping time.


    public static void XSquaredAnalysis()
	{
        /*
         * See LinearAnalysis() for more details!
         */

        Console.WriteLine("Welcome to the X-Squared Test Equation Space");

        //GET SCHEME TYPE
        eulerType = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which scheme should be used?")                      
                    .AddChoices("Explicit", "Implicit", "Theta", "Error Compare"));           

        AnsiConsole.MarkupLine($"You picked [blue]{eulerType}[/]");             //f(X,t) is what dX/dt is equal to (mX^2 here) (m is lambda)

        switch (eulerType)
        {
            case "Explicit":
                thetaValue = 1d;
                break;

            case "Implicit":
                thetaValue = 0d;
                break;

            case "Theta":
                //THETA PROMPT
                var thetaPrompt = new TextPrompt<double>("What will be the [blue]theta value[/] (use 0.5 for trapezium)?")
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
                thetaBattle = true;
                break;

            default:
                break;

        }
        //LAMBDA PROMPT
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




        // START TIME PROMPT
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


        /*
         * TIMESTEP PROMPT
         * When theta is 0, there is no limitation on Δt (apart from it being greater than 0 of course).
         * For all other theta, Δt < C/θ, where C is the start time.
         * Interestingly that bound is independent of λ.
         */
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



        // STOPTIME PROMPT
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


        //TABLE
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
        /*
         * Similar to LinearIteration(), a while loop is used, but rather than just a multiplier applied to currentX...
         * ... a method is called to apply currentX into the relevant formula.
         * 
         * See LinearIteration() for more details.
         */

        if (!thetaBattle)
        {
            AnsiConsole.MarkupLine($"[green bold]Beginning Approximation[/], Initial value X_0 = X({customStart}) = {(lambdaValue > 0 ? -1d/(lambdaValue*customStart) : 1d / (lambdaValue * customStart))}");
        }

        // a and b can now be set
        a = (1d - thetaValue) * (lambdaValue) * (timestepValue);
        b = (thetaValue) * (lambdaValue) * (timestepValue);

        //compute the starting value of customStartIndex
        customStartIndex = customStart + (iterN * timestepValue);

        ResetIterationXSquared();

        while (customStartIndex <= timeStopPoint)
        {
            timeAxis.Add(customStartIndex);
            approxPoints.Add(currentX);
            exactPoints.Add(currentEXact);
            errorValuesAbs.Add(Math.Abs(currentX - currentEXact));
            errorValuesRel.Add(Math.Abs(currentX - currentEXact) / Math.Abs(currentEXact));

            //test point, printing values (explicit with lambda = 1 and timestep = 1 should be a doubler)
            //Console.WriteLine($"at time {customStartIndex}, the approx value of -1 over {lambdaValue}t is {currentX} and the true value is {currentEXact}");

            //Update customStartIndex value when incrementing iterN
            iterN++; customStartIndex = customStart + (iterN * timestepValue);

            //A ternary operator is used to direct to the correct formula
            currentX = thetaValue == 1d ? ExplicitFormula(currentX) : OtherFormula(currentX);
            currentEXact = -1d / (lambdaValue * (customStartIndex));
        }

        //testpoint, print the global (maximum error)
        //Console.WriteLine($"the global error was: {errorValuesAbs.Max()}, and the global relative error was: {errorValuesRel.Max()}");

        if (!thetaBattle)
        {
            AnsiConsole.MarkupLine("[green bold]DONE![/]");
            XSquaredResultPlots();
        }
    }


    private static double ExplicitFormula(double xN)
    {
        return xN + (b * Math.Pow(xN, 2d));
    }

    private static double OtherFormula(double xN)
    {
        double determinant = 1d - (4d * a * (xN + (b * Math.Pow(xN,2d))));

        double rootDet = Math.Sqrt(determinant);

        return (1d - rootDet) / (2d * a);
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
        /*
         * This method is very similar to LinearResultPlots(), only the function being plotted is really different, along with some axis bounds...
         * ... see LinearResultPlots for more details
         */

        //COMPARISON PLOT
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

        xSquaredResults.ShowLegend();


        //ERROR PLOT
        ScottPlot.Plot xSquaredErrors = new();
        var relErrorCurve = xSquaredErrors.Add.SignalXY(timeAxis, errorValuesRel, ScottPlot.Color.FromHex("0000ff"));

        xSquaredErrors.XLabel("t");
        xSquaredErrors.YLabel("Relative Error");
        xSquaredErrors.Title($"Relative Error in xSTE {(thetaValue == 0.5 ? "Trapezium" : eulerType)} FD Scheme {((thetaValue != 0d & thetaValue != 0.5d & thetaValue != 1d) ? "(Theta = " + thetaValue + ")" : "")}in timesteps of {timestepValue}: dX/dt = {((lambdaValue == 1d) ? "" : lambdaValue)}X^2 | X({customStart}) = {(lambdaValue > 0 ? -1d / (lambdaValue * customStart) : 1d / (lambdaValue * customStart))}");

        xSquaredErrors.Axes.MarginsY(0);
        xSquaredErrors.Axes.AutoScaleY();
        xSquaredErrors.Axes.SetLimitsX(0, timeStopPoint);
        

        //SAVE PLOTS
        string finalPath = DetermineSaveLocation();

        //decided against prompting for custom image names for now
        string finalResultsPath = Path.Combine(finalPath, "xSquaredResultsPLOT.png");
        string finalErrorPath = Path.Combine(finalPath, "xSquaredErrorPLOT.png");

        xSquaredResults.SavePng(finalResultsPath, 1280, 720);
        xSquaredErrors.SavePng(finalErrorPath, 1280, 720);
    }


    private static void XSThetaBattle()
    {
        /*
         * This is the Error Compare method for this function, see ThetaBattle for more details.
         */
 
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
