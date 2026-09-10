# ThetaEulerMethods



Welcome to my Numerical Method simulation!

It's based on a simple example from my PDE course, the Euler iteration schemes on the Linear test equation (dX/dt = λx, with X(0) = 1)

These schemes are actually a simple version of the Runge-Kutta approximation schemes!

The iterative scheme looks like this: (X\_n+1 - X\_n)/ Δt = θf(X\_n,t\_n) + (1-θ)f(X\_n+1,t\_n+1), where t\_n = nΔt

I decided to create this as a fun exercise to have a quick way to simulate whatever conditions I wanted, and to visualise the results.

I was also able to implement the schemes for a different equation (dX/dt = λX^2, with initial condition such that the true solution would have no constant term)

Of course these are normally easily solved ODEs, but it's still cool to see these approximate schemes at work...

...and it means I can collect global error information to compare specific kinds of Euler schemes.

&#x20;

The basic structure of this program is as follows:

1: Select an equation

2: Choose the Euler scheme (represented by a value θ ∈ \[0,1]) (0 corresponds to implicit, 1 to explicit, and 0.5 to trapezium)

3: Enter the variables dictating what the domain of approximation will be, and the coefficient of the function, and the timestep (Δt)

4: Let the simulation do its thing

5: Choose a save location for the plots relative to your Pictures folder

&#x20;<img width="1280" height="720" alt="linearResultsPLOT" src="https://github.com/user-attachments/assets/a73cbcad-8e2d-4945-b58d-343af251dfa8" />


There are 2 plots: one for the function and the approximation, and another for the relationship between time and relative error

There is also an Error Compare option that runs the simulation 101 times for theta values evenly spaced on \[0,1]...

...then plotting each theta against the maximum attained relative error. That was an idea I got while working on this.

&#x20;

I may add a third kind of function in the future, that includes the t variable not yet used in the RHS.

