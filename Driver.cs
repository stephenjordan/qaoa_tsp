using System;
using System.Linq;
using Microsoft.Quantum.Simulation.Core;
using Microsoft.Quantum.Simulation.Simulators;

namespace Quantum.QAOA
{
    class Program
    {

        // Calculate the cost of Santa's journey
        // segmentCosts defines the cost of each potential segment of the journey
        // segmentUsed indicates whether the segment was part of the itinerary
        static double Cost(double[] segmentCosts, bool[] segmentUsed)
        {
            var totalCost = 0.0;
            for (var i = 0; i < segmentCosts.Length; ++i) {
                if (segmentUsed[i]) {
                    totalCost += segmentCosts[i];
                }
            }
            return totalCost;
        }

        // If the proposed string of boolean values satisfies all of the constraints and therefore
        // represents a valid loop through the destinations, return true. Otherwise return false.
        static bool Satisfactory(bool[] r)
        {
            var HammingWeight = 0;
            for (int i = 0; i < 6; i++)
            {
                if (r[i]) HammingWeight++;
            }
            if (HammingWeight != 4) return false;
            if (r[0] != r[2]) return false;
            if (r[1] != r[3]) return false;
            if (r[4] != r[5]) return false;
            return true;
        }

        static void Main(string[] args)
        {
            // We start by loading the simulator that we will use to run our Q# operations.
            using (var qsim = new QuantumSimulator())
            {
                Console.WriteLine("Starting simulation\n");

                // Define the costs of journey segments
                double[] segmentCosts = { 4.70, 9.09, 9.03, 5.70, 8.02, 1.71 };
                // Define the penalty for constraint violation
                double penalty = 20.0;

                // Here are some magic QAOA parameters that we got by lucky guessing.
                // Theoretically, they should yield the optimal solution in 70.6% of trials.
                double[] dtx = { 0.619193, 0.742566, 0.060035, -1.568955, 0.045490 };
                double[] dtz = { 3.182203, -1.139045, 0.221082, 0.537753, -0.417222 };

                // Convert parameters to QArray<Double> to pass them to Q#
                var tx = new QArray<Double>(dtx);
                var tz = new QArray<Double>(dtz);
                var costs = new QArray<Double>(segmentCosts);

                var bestCost = 100.0 * penalty;
                var bestItinerary = new bool[6];
                var successNumber = 0;
                for (int trial = 0; trial < 20; trial++)
                {
                    var result = QAOA_santa.Run(qsim, costs, penalty, tx, tz, 5).Result;
                    var tmp = result.ToArray<bool>();
                    var cost = Cost(segmentCosts, tmp);
                    var sat = Satisfactory(tmp);
                    Console.WriteLine($"result = {result}, cost = {cost}, satisfactory = {sat}");
                    if (sat) {
                        if (cost < bestCost - 1E-6) {
                            // New best cost found - update
                            bestCost = cost;
                            Array.Copy(tmp, bestItinerary, 6);
                            successNumber = 1;
                        } else if (Math.Abs(cost - bestCost) < 1E-6) {
                            successNumber++;
                        }
                    }
                }
                Console.WriteLine("Simulation is complete\n");
                Console.WriteLine($"Best itinerary found: {bestItinerary}, cost = {bestCost}");
                Console.WriteLine($"{successNumber * 100.0 / 20}% of runs found the best itinerary\n");
                Console.WriteLine("Press any key to continue\n");
                Console.ReadKey();
            }
        }
    }
}