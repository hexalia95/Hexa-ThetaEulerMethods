# ThetaEulerMethods



&#x20;\* Welcome to my Numerical Method simulation!

&#x20;\* It's based on a simple example from my PDE course, the Euler iteration schemes on the Linear test equation (dX/dt = λx, with X(0) = 1)

&#x20;\* The iterative scheme looks like this: (X\_n+1 - X\_n)/ Δt = θf(X\_n,t\_n) + (1-θ)f(X\_n+1,t\_n+1), where t\_n = nΔt  

&#x20;\* I decided to create this as a fun exercise to have a quick way to simulate whatever conditions I wanted, and to visualise the results.

&#x20;\* I was also able to implement the schemes for a different equation (dX/dt = λX^2, with initial condition such that the true solution would have no constant term)

&#x20;\* Of course these are normally easily solved ODEs, but it's still cool to see these approximate schemes at work... 

&#x20;\* ...and it means I can collect global error information to compare specific kinds of Euler schemes.

&#x20;

&#x20;\* The basic structure of this program is as follows:

&#x20;\* 1: Select an equation

&#x20;\* 2: Choose the Euler scheme (represented by a value θ ∈ \[0,1]) (0 corresponds to implicit, 1 to explicit, and 0.5 to trapezium)

&#x20;\* 3: Enter the variables dictating what the domain of approximation will be, and the coefficient of the function, and the timestep (Δt)

&#x20;\* 4: Let the simulation do its thing

&#x20;\* 5: Choose a save location for the plots relative to your Pictures folder

&#x20;

&#x20;\* There are 2 plots: one for the function and the approximation, and another for the relationship between time and relative error

&#x20;\* There is also an Error Compare option that runs the simulation 101 times for theta values evenly spaced on \[0,1]...

&#x20;\* ...then plotting each theta against the maximum attained relative error. That was an idea I got while working on this.

&#x20;

&#x20;\* I may add a third kind of function in the future, that includes the t variable not yet used in the RHS.



