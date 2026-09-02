using ScottPlot;
using Spectre.Console;
using System;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using ThetaEulerMethods;

public class LinearTestEquation    //new file for organised structure
{
    /*
     * Here is the class file for the Linear test equation (LTE)
     * As expected everything here is static because this class isn't really being used to make any actual objects
     * The X Squared test equation class actually derives from this one , for the sake of 2 methods that can work for any function.
     * 
     * The true solution for the LTE "dX/dt = λx, with X(0) = 1" is X(t) = e^(λt)
     * The approximation recurrence is given by: X_n+1 = X_n * [(1+(θλΔt)) / (1-(1-θ)λΔt)]
     * 
     * I will explain the function of each method here just inside their declarations, and the variables alongside their declaration
     */

    static string? eulerType;                       //Stores the chosen Euler option (will be used when titling the plots)
    static bool thetaBattle = false;                //Stores whether an Error Compare should be performed

    static double thetaValue;                       //Stores the theta: it must be inputted manually if Explicit (=1) or Implicit (=0) weren't chosen
    static double lambdaValue;                      //Stores the coefficient: must be nonzero
    static double timestepValue;                    //Stores Δt, will affect the number of iterations that need to be computed
    static double stabilityFactor;                  //Exclusive to this LTE scheme, determines the upper bound on Δt if the Explicit mode is chosen
    static double timeStopPoint;                    //Stores the chosen stop time (eg. after 10 seconds). Note that time based variables are always in seconds.

    static List<double> timeAxis = [];              //List of time points (our eventual x-axis)
    static List<double> approxPoints = [];          //Stores the values from the iterative process (X_n) (our y-axis)
    static List<double> exactPoints = [];           //Stores the exact values of the function e^(λnΔt)
    static List<double> errorValuesAbs = [];        //Stores the absolute errors between each pair (exact and approx values)
    static List<double> errorValuesRel = [];        //Stores the relative errors between each pair

    static double iterativeMultiplier;              //The multiplier applied at each step of the iteration
    static int iterN = 0;                           //Indexer to control our iteration (the n in nΔt)
    static double currentX = 1d;                    //currentX is X_n in our approximation, and X_0 is always 1 in LTE
    static double currentEXact = 1d;                //currentEXact is the true value of e^(λnΔt)


    //ERROR COMPARE EXCLUSIVE
    static readonly List<double> thetaSpaced = [.. Generate.Consecutive(101, 0.01, 0)];  //Stores [0,0.01,0.02,...,0.99,1]
    static List<double> globalErrors = new(101);                                         //Stores the maximum relative errors for each simulation under the thetas given above



    public static void LinearAnalysis()     //initialize the values and scheme that will be used for the iteration
    {

        /*
         * This is where the user is first sent to after selecting a function.
         * The program will prompt the user for θ (or to error compare), λ, Δt and a stopping time, and summarise these choices in a table.
         * These choices are validated using Spectre.Console's Direct TextPrompt Validation, to impose conditions on the input and...
         * ...output custom messages when they are violated.
         */

        Console.WriteLine("Welcome to the Linear Test Equation Space");

        eulerType = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which scheme should be used?")
                    .AddChoices("Explicit", "Implicit", "Theta", "Error Compare"));

        AnsiConsole.MarkupLine($"You picked [blue]{eulerType}[/]");

        //Choosing the Explicit or Implicit options will set θ automatically, else requiring a custom input.
        //Again a switch block is used to interpret the user's choice.
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
                //Set the flag to pivot the program to the Error Compare methods later.
                thetaBattle = true;
                break;

            default:
                break;

        }

        //LAMBDA PROMPT
        var lambdaPrompt = new TextPrompt<double>("What will be the [blue]co-efficient[/]? [green](dX/dt = λX)[/]")
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


        /*
         * Only the Explicit scheme has a Δt limitaton, when λ < 0. The other theta values are unconditional. However...
         * ...when λ > 0, these other theta values can run into division-by-zero problems.
         */
        stabilityFactor = 2d / Math.Abs(lambdaValue);        

        //TIMESTEP PROMPT
        var timestepPrompt = new TextPrompt<double>("What will be the [blue]time step[/]? [green](X_n approximates X(n * Δt))[/]")
            .Validate(input =>
            {
                if (input <= 0)
                {
                    return ValidationResult.Error("[red]Timestep must be positive![/]");
                }

                if (input >= stabilityFactor & thetaValue == 1 || input >= stabilityFactor & thetaBattle == true)
                {
                    return ValidationResult.Error($"[red]In the explicit scheme, the timestep must be sufficiently small to guarantee stability! (<{stabilityFactor})[/]");
                }

                if (input * lambdaValue * (1d - thetaValue) == 1)
                {
                    return ValidationResult.Error($"[red] There will be a division by 0 problem if you use this value![/]");
                }

                return ValidationResult.Success();
            });

        timestepValue = AnsiConsole.Prompt(timestepPrompt);
        AnsiConsole.MarkupLine($"You chose a timestep of [blue]{timestepValue}[/]");


        /*
         * The iteration will run until n * Δt > the chosen stopping point.
         */

        //STOPTIME PROMPT
        var stopPointPrompt = new TextPrompt<double>("When should the [blue]simulation[/] end? [green](Time in seconds))[/]")
            .Validate(input =>
            {
                if (input <= 0)
                {
                    return ValidationResult.Error("[red]Time must be positive![/]");
                }

                if (input < timestepValue)
                {
                    return ValidationResult.Error($"[red]Timestep exceeds the stopping point![/]");
                }

                return ValidationResult.Success();
            });

        timeStopPoint = AnsiConsole.Prompt(stopPointPrompt);
        AnsiConsole.MarkupLine($"You chose a stop time of [blue]{timeStopPoint}[/]");



        /*
         * Spectre.Console has Table functionality!
         * A table is created to summarise values before creating the iteration
         * It's basic for now, can stylise it later
         */

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
        table.AddRow("Stopping Time", $"{timeStopPoint}");


        Console.WriteLine();
        AnsiConsole.Write(table);

        Console.WriteLine("Press Enter to Begin!");
        Console.ReadLine();


        //Here the bool of thetaBattle is checked: the ThetaBattle method sets up a loop of LinearIteration calls.
        if (!thetaBattle)
        {
            LinearIteration();
        }
        else
        {
            AnsiConsole.MarkupLine("[green bold] Beginning theta comparison![/]");
            ThetaBattle();
        }
    }


    private static void LinearIteration()
    {
        /*
         * Here is where the main simulation loop occurs.
         * The multiplier [(1+(θλΔt)) / (1-(1-θ)λΔt)] is stored to a variable, then ResetIteration() is called.
         * What that does is clear out every list and sets the currentX, currentEXact and iterN variables to their defaults.
         * It's most relevant for the Error Compare option, where this function is called 101 times, and the aforementioned variables need to be reset for each simulation.
         * 
         * Anyway, the while loop is used to count time, from 0, in increments of Δt, until the Stopping Time is reached.
         * During each loop, the lists are updated to contain new values at each time step. Then iterN is incremented, allowing for the next currentX and currentEXact to be calculated.
         * Finally, X_n is multiplied by iterativeMultiplier to obtain X_n+1, and the next currentEXact value is calculated directly.
         */


        if (!thetaBattle)
        {
            AnsiConsole.MarkupLine($"[green bold]Beginning Approximation[/], Initial value X_0 = X(0) = 1");
        }


        //STORE MULTIPLIER
        iterativeMultiplier = (1d + (thetaValue * lambdaValue * timestepValue)) / (1d - ((1d - thetaValue) * lambdaValue * timestepValue));

        //RESET ITERATION
        ResetIteration();

        //RUN SIMULATION
        while (iterN * timestepValue <= timeStopPoint)
        {
            //UPDATE LISTS
            timeAxis.Add(iterN * timestepValue);
            approxPoints.Add(currentX);
            exactPoints.Add(currentEXact);
            errorValuesAbs.Add(Math.Abs(currentX - currentEXact));
            errorValuesRel.Add(Math.Abs(currentX - currentEXact) / currentEXact);

            //test point, printing values (explicit with lambda = 1 and timestep = 1 should be a doubler)
            //Console.WriteLine($"at time {iterN * timestepValue}, the approx value of e to the {lambdaValue}t is {currentX} and the true value is {currentEXact}");


            //GET NEW APPROX AND EXACT VALUES
            iterN++;
            currentX *= iterativeMultiplier;
            currentEXact = Math.Pow(Math.E, iterN * timestepValue * lambdaValue);
        }

        //testpoint, print the global (maximum error)
        //Console.WriteLine($"the global error was: {errorValuesAbs.Max()}, and the global relative error was: {errorValuesRel.Max()}");


        //There are 2 methods for plotting results: Error Compare doesn't use LinearResultPlots()
        if (!thetaBattle)
        {
            AnsiConsole.MarkupLine("[green bold]DONE![/]");
            LinearResultPlots();
        }
    }


    private static void ResetIteration()
    {
        timeAxis.Clear();
        exactPoints.Clear();
        approxPoints.Clear();
        errorValuesAbs.Clear();
        errorValuesRel.Clear();
        iterN = 0;
        currentX = 1d;
        currentEXact = 1d;
    }


    private static void LinearResultPlots()
    {
        /*
         * ScottPlot is used to generate the plot for the exact and approximated curves on the same graph, time on the x-axis, X(t) on the y-axis.
         * Another plot is also made - for the graph of relative error against time.
         * SignalXY plots are much more performant than scatters given there can be many thousands of values in these lists to plot.
         * 
         * 
         * The graphs are a bit basic in appearance for now, but they can be stylised later. For now the axis margins and labels, title, and legend are included.
         * The title uses a long interpolated string to display the correct information each time.
         * 
         * The last part of this method gets the save location for the png files the plots will be stored to.
         * DetermineSaveLocation gets a user input for folder pathing relative to their Pictures folder, which is then used in the ScottPlot 'SavePng' method to save the plots...
         * ... as a 1280x720 image in the chosen location.
         */


        //CREATE COMPARISON PLOT
        ScottPlot.Plot linearResults = new();
        var exactCurve = linearResults.Add.SignalXY(timeAxis, exactPoints, ScottPlot.Color.FromHex("ff0000"));
        var approxCurve  = linearResults.Add.SignalXY(timeAxis,approxPoints, ScottPlot.Color.FromHex("00ff00"));

        exactCurve.LegendText = "Exact";
        approxCurve.LegendText = "Approx.";
        linearResults.Legend.Alignment = Alignment.UpperCenter;

        linearResults.Axes.MarginsY(0.1);
        linearResults.Axes.AutoScaleY();
        linearResults.Axes.SetLimitsX(0, timeStopPoint);

        linearResults.XLabel("t");
        linearResults.YLabel("X");
        linearResults.Title($"LTE {(thetaValue == 0.5 ? "Trapezium" : eulerType)} FD Scheme {((thetaValue != 0d & thetaValue != 0.5d & thetaValue !=  1d) ? "(Theta = " + thetaValue + ")" : "")}in timesteps of {timestepValue}: dX/dt = {((lambdaValue == 1d) ? "" : lambdaValue)}X | X(0) = 1");

        linearResults.ShowLegend();
        

        //CREATE ERROR PLOT
        ScottPlot.Plot linearErrors = new();
        var relErrorCurve = linearErrors.Add.SignalXY(timeAxis, errorValuesRel, ScottPlot.Color.FromHex("0000ff"));

        linearErrors.XLabel("t");
        linearErrors.YLabel("Relative Error");
        linearErrors.Title($"Relative Error in LTE {(thetaValue == 0.5 ? "Trapezium" : eulerType)} FD Scheme {((thetaValue != 0d & thetaValue != 0.5d & thetaValue != 1d) ? "(Theta = " + thetaValue + ")" : "")}in timesteps of {timestepValue}: dX/dt = {((lambdaValue == 1d) ? "" : lambdaValue)}X | X(0) = 1");

        linearErrors.Axes.MarginsY(0.1);
        linearErrors.Axes.AutoScaleY();
        linearErrors.Axes.SetLimitsX(0, timeStopPoint);


        
        //SAVE THE IMAGES
        string finalPath = DetermineSaveLocation();

        //Custom image names in the future maybe
        string finalResultsPath = Path.Combine(finalPath, "linearResultsPLOT.png");
        string finalErrorPath = Path.Combine(finalPath, "linearErrorPLOT.png");


        linearResults.SavePng(finalResultsPath, 1280, 720);
        linearErrors.SavePng(finalErrorPath, 1280, 720);
    }


    protected static string DetermineSaveLocation() 
    {
        /*
         * This methods appends a user input to the environment's special Pictures folder, and returns the result.
         * Whitespace is removed to prevent issues when using Path.Combine()
         * The directory is then created, if it didn'y already exist.
         */

        var pictureFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        var savePrompt = new TextPrompt<string>(@"Where should the plot data be saved in the Pictures folder? Example: 'Extras\Plots', or leave empty").AllowEmpty();
        var saveLocation = AnsiConsole.Prompt(savePrompt).Trim();

        string finalPath = Path.Combine(pictureFolder, saveLocation);

        if (!Path.Exists(finalPath))
        {
            Directory.CreateDirectory(finalPath);
        }

        return finalPath;
    }

    private static void ThetaBattle()
    {
        /*
         * Here's the method that runs the Error Compare Option. The for loop sets each theta value in hundredths from 0 to 1 and...
         * ...calls LinearIteration for each one. Recall that the lambda, timestep and stopping point were all already determined during LinearAnalysis().
         * 
         * The globalErrors List sees use here, to store the maximum value in errorValuesRel for each simulation; this is what will be compared.
         * 
         * The final part of this method declares the title for the upcoming plot method, exclusive to Error Compare.
         * This allows for other equation scheme to use that method without needing to write out very similar code.
         */

        //RUN ERROR COMPARE
        for (int i = 0; i <= 100; i++)
        {
            thetaValue = i / 100d;  
            LinearIteration();

            globalErrors.Add(errorValuesRel.Max());
        }

        //testpoint, have values been collected correctly?
        /*foreach (double value in thetaSpaced)
        {
            Console.WriteLine($"at theta = {value}, the global error was {globalErrors[thetaSpaced.IndexOf(value)]}");
        }*/

        string titlestring = $"Error Comparison in LTE FD Schemes of differing theta values for equation (in timesteps of {timestepValue}): dX/dt = {((lambdaValue == 1d) ? "" : lambdaValue)}X | X(0) = 1";
        ThetaBattlePlots(titlestring);
    }


    protected static void ThetaBattlePlots(string title)
    {
        /*
         * This method is similar to LinearResultPlots(). Just as in there, the plot here is basic and can be stylised later.
         * Again the plot is a SignalXY for similar reasons.
         * 
         * The plot is also saved in the same way as the other method.
         */
        

        //CREATE THETA COMPARISON PLOT
        ScottPlot.Plot thetaPlot = new();
        var thetaCurve = thetaPlot.Add.SignalXY(thetaSpaced, globalErrors, ScottPlot.Color.FromHex("ffb700"));
        thetaCurve.LineWidth = 5;

        thetaPlot.XLabel("Theta Value (Implicit -> Explicit)");
        thetaPlot.YLabel("Maximum Relative Error");
        thetaPlot.Title(title);

        thetaPlot.Axes.SetLimitsX(0, 1);
        thetaPlot.Axes.SetLimitsY(0, globalErrors.Max());


        //SAVE THE PLOT
        string finalPath = DetermineSaveLocation();
        string finalThetaBattlePath = Path.Combine(finalPath, "errorComparisonPLOT.png");

        thetaPlot.SavePng(finalThetaBattlePath, 1280, 720);
    }


    public LinearTestEquation()
	{
		
	}
}
