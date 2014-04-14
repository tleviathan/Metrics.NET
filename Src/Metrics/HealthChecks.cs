﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Metrics.Core;

namespace Metrics
{
    /// <summary>
    /// Structure describing the status of executing all the health checks operations.
    /// </summary>
    public struct HealthStatus
    {
        /// <summary>
        /// All health checks passed.
        /// </summary>
        public readonly bool IsHealty;

        /// <summary>
        /// Result of each health check operation
        /// </summary>
        public readonly HealthCheck.Result[] Results;

        public HealthStatus(IEnumerable<HealthCheck.Result> results)
        {
            this.Results = results.ToArray();
            this.IsHealty = this.Results.All(r => r.Check.IsHealthy);
        }
    }

    /// <summary>
    /// Registry for health checks
    /// </summary>
    public static class HealthChecks
    {
        private static readonly ConcurrentDictionary<string, HealthCheck> checks = new ConcurrentDictionary<string, HealthCheck>();

        /// <summary>
        /// Registers an action to monitor. If the action throws the health check fails, otherwise is successful.
        /// </summary>
        /// <param name="name">Name of the health check.</param>
        /// <param name="check">Action to execute.</param>
        public static void RegisterHealthCheck(string name, Action check)
        {
            RegisterHealthCheck(new HealthCheck(name, check));
        }

        /// <summary>
        /// Registers an action to monitor. If the action throws the health check fails, 
        /// otherwise is successful and the returned string is used as status message.
        /// </summary>
        /// <param name="name">Name of the health check.</param>
        /// <param name="check">Function to execute.</param>
        public static void RegisterHealthCheck(string name, Func<string> check)
        {
            RegisterHealthCheck(new HealthCheck(name, check));
        }

        /// <summary>
        /// Registers a function to monitor. If the function throws or returns an HealthCheckResult.Unhealthy the check fails,
        /// otherwise the result of the function is used as a status.
        /// </summary>
        /// <param name="name">Name of the health check.</param>
        /// <param name="check">Function to execute</param>
        public static void RegisterHealthCheck(string name, Func<HealthCheckResult> check)
        {
            RegisterHealthCheck(new HealthCheck(name, check));
        }

        /// <summary>
        /// Registers a custom health check.
        /// </summary>
        /// <param name="check">Custom health check to register.</param>
        public static void RegisterHealthCheck(HealthCheck check)
        {
            checks.AddOrUpdate(check.Name, check, (n, c) => check);
        }

        /// <summary>
        /// Execute all registered checks and return overall.
        /// </summary>
        /// <returns>Status of the system.</returns>
        public static HealthStatus GetStatus()
        {
            var results = checks.Values.Select(v => v.Execute()).OrderBy(r => r.Name);
            return new HealthStatus(results);
        }
    }
}
