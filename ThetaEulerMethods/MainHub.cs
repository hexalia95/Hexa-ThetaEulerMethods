using Spectre.Console;
using System.ComponentModel.DataAnnotations;

namespace ThetaEulerMethods
{
    internal class MainHub
    {

        static void Main(string[] args)
        {
            /*
             * Welcome to my Numerical Method simulation!
             * It's based on a simple example from my PDE course, the Euler iteration schemes on the Linear test equation (dX/dt = λx, with X(0) = 1)
             * These schemes are actually a simple version of the Runge-Kutta approximation schemes!
             * The iterative scheme looks like this: (X_n+1 - X_n)/ Δt = θf(X_n,t_n) + (1-θ)f(X_n+1,t_n+1), where t_n = nΔt  
             * I decided to create this as a fun exercise to have a quick way to simulate whatever conditions I wanted, and to visualise the results.
             * I was also able to implement the schemes for a different equation (dX/dt = λX^2, with initial condition such that the true solution would have no constant term)
             * Of course these are normally easily solved ODEs, but it's still cool to see these approximate schemes at work... 
             * ...and it means I can collect global error information to compare specific kinds of Euler schemes.
             * 
             * The basic structure of this program is as follows:
             * 1: Select an equation
             * 2: Choose the Euler scheme (represented by a value θ ∈ [0,1]) (0 corresponds to implicit, 1 to explicit, and 0.5 to trapezium)
             * 3: Enter the variables dictating what the domain of approximation will be, and the coefficient of the function, and the timestep (Δt)
             * 4: Let the simulation do its thing
             * 5: Choose a save location for the plots relative to your Pictures folder
             * 
             * There are 2 plots: one for the function and the approximation, and another for the relationship between time and relative error
             * There is also an Error Compare option that runs the simulation 101 times for theta values evenly spaced on [0,1]...
             * ...then plotting each theta against the maximum attained relative error. That was an idea I got while working on this.
             * 
             * I may add a third kind of function in the future, that includes the t variable not yet used in the RHS.
             * 
             * I will document the code here as well as I can!
             */


            /*
             * Spectre.Console is used for easy console menu creation, simply needing to provide a title and listing of choices of which the chosen one is stored to the variable.
             * I can then use a switch statement to direct the program to the appropriate class to perform the relevant simulation based on the chosen option string.
             */

            Console.WriteLine("Let's get to it!");

            var testEquation = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Which Equation will you approximate?")
                    .AddChoices("Linear","X Squared"));

            AnsiConsole.MarkupLine($"You picked [blue]{testEquation}[/]");

            /*
             * Spectre.Console also adds markup functionality for text; nothing major, but it does break up the usual white monotony which can improve readability.
             * The simulation spaces for each equation are stored in their own class files to keep things mostly organised.
             */

            switch (testEquation)
            {
                case "Linear":
                    //Implemented!
                    LinearTestEquation.LinearAnalysis();

                    AnsiConsole.MarkupLine($"[green]Bye Bye[/]");
                    break;

                case "X Squared":
                    //Implemented!
                    XSquaredTestEquation.XSquaredAnalysis();

                    AnsiConsole.MarkupLine($"[green]Bye Bye V2[/]");
                    break;

                default:
                    break;
            }

            Console.ReadLine();
        }
    }
}
