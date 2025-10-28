using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static List<string> Solve(List<(string, string)> edges)
    {
        var edgeSet = new HashSet<string>();

        foreach (var (u, v) in edges)
            edgeSet.Add(String.CompareOrdinal(u, v) <= 0 ? $"{u}-{v}" : $"{v}-{u}");

        var finalSeq = Dfs("a", edgeSet);
        return finalSeq;
    }

    static List<string> Dfs(string virus, HashSet<string> edges)
    {
        var memoFail = new HashSet<string>();
        var memoSuccess = new Dictionary<string, List<string>>();

        var key = StateKey(virus, edges);
        if (memoFail.Contains(key))
            return null;
        if (memoSuccess.TryGetValue(key, out var seq))
            return new List<string>(seq);

        var adj = BuildAdjacency(edges);

        var (closestGateway, distToGateway) = FindClosestGateway(virus, adj);
        if (closestGateway == null)
        {
            memoSuccess[key] = new List<string>();
            return new List<string>();
        }

        var candidates = new List<(string gateway, string node)>();
        foreach (var e in edges)
        {
            var parts = e.Split('-');
            var x = parts[0];
            var y = parts[1];
            if (IsGateway(x) && !IsGateway(y))
            {
                candidates.Add((x, y));
            }
            else if (IsGateway(y) && !IsGateway(x))
            {
                candidates.Add((y, x));
            }
        }

        candidates.Sort((p, q) =>
        {
            var c = String.CompareOrdinal(p.gateway, q.gateway);
            if (c != 0)
                return c;
            return String.CompareOrdinal(p.node, q.node);
        });

        foreach (var cand in candidates)
        {
            var nextEdges = new HashSet<string>(edges);
            var normalized = String.CompareOrdinal(cand.gateway, cand.node) <= 0 ? $"{cand.gateway}-{cand.node}" :
                                                                                   $"{cand.node}-{cand.gateway}";
            if (!nextEdges.Remove(normalized))
                continue;

            var adjAfter = BuildAdjacency(nextEdges);

            var (closestAfter, distAfter) = FindClosestGateway(virus, adjAfter);
            if (closestAfter == null)
            {
                var resultSeq = new List<string> { $"{cand.gateway}-{cand.node}" };
                memoSuccess[key] = new List<string>(resultSeq);
                return resultSeq;
            }

            var distFromGateway = BFSDistancesFrom(closestAfter, adjAfter);
            if (!distFromGateway.ContainsKey(virus))
            {
                var resultSeq = new List<string> { $"{cand.gateway}-{cand.node}" };
                memoSuccess[key] = new List<string>(resultSeq);
                return resultSeq;
            }


            var dCur = distFromGateway[virus];
            string chosenNext = null;
            if (dCur == 0)
            {
                chosenNext = closestAfter;
            }
            else
            {
                if (!adjAfter.ContainsKey(virus))
                {
                    var resultSeq = new List<string> { $"{cand.gateway}-{cand.node}" };
                    memoSuccess[key] = new List<string>(resultSeq);
                    return resultSeq;
                }

                var possible = adjAfter[virus]
                    .Where(nb => distFromGateway.ContainsKey(nb) && distFromGateway[nb] == dCur - 1)
                    .OrderBy(s => s, StringComparer.Ordinal)
                    .ToList();

                if (possible.Count == 0)
                {
                    var rec = Dfs(virus, nextEdges);

                    if (rec != null)
                    {
                        var res = new List<string> { $"{cand.gateway}-{cand.node}" };
                        res.AddRange(rec);
                        memoSuccess[key] = new List<string>(res);
                        return res;
                    }
                    else
                    {
                        continue;
                    }
                }

                chosenNext = possible.First();
            }

            if (IsGateway(chosenNext))
                continue;

            var recResult = Dfs(chosenNext, nextEdges);
            if (recResult != null)
            {
                var res = new List<string> { $"{cand.gateway}-{cand.node}" };
                res.AddRange(recResult);
                memoSuccess[key] = new List<string>(res);
                return res;
            }
        }

        memoFail.Add(key);
        return null;
    }

    static bool IsGateway(string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;
        return char.IsUpper(s[0]);
    }

    static Dictionary<string, List<string>> BuildAdjacency(HashSet<string> edges)
    {
        var adj = new Dictionary<string, List<string>>();
        foreach (var e in edges)
        {
            var p = e.Split('-');
            var u = p[0];
            var v = p[1];
            if (!adj.ContainsKey(u))
                adj[u] = new List<string>();
            if (!adj.ContainsKey(v))
                adj[v] = new List<string>();
            if (!adj[u].Contains(v))
                adj[u].Add(v);
            if (!adj[v].Contains(u))
                adj[v].Add(u);
        }
        foreach (var key in adj.Keys.ToList())
            adj[key].Sort(StringComparer.Ordinal);
        return adj;
    }

    static (string gateway, int dist) FindClosestGateway(string start, Dictionary<string, List<string>> adj)
    {
        if (!adj.ContainsKey(start))
            adj[start] = new List<string>();
        var q = new Queue<string>();
        var dist = new Dictionary<string, int>();
        q.Enqueue(start);
        dist[start] = 0;
        var bestDist = int.MaxValue;
        var bestGates = new List<string>();

        while (q.Count > 0)
        {
            var u = q.Dequeue();
            var du = dist[u];
            if (du > bestDist)
                continue;

            if (IsGateway(u))
            {
                if (du < bestDist)
                {
                    bestDist = du;
                    bestGates.Clear();
                    bestGates.Add(u);
                }
                else if (du == bestDist)
                {
                    bestGates.Add(u);
                }
                continue;
            }

            if (!adj.ContainsKey(u))
                continue;
            foreach (var nb in adj[u])
            {
                if (!dist.ContainsKey(nb))
                {
                    dist[nb] = du + 1;
                    q.Enqueue(nb);
                }
            }
        }

        if (bestGates.Count == 0)
            return (null, -1);

        bestGates.Sort(StringComparer.Ordinal);

        return (bestGates[0], bestDist);
    }

    static Dictionary<string, int> BFSDistancesFrom(string start, Dictionary<string, List<string>> adj)
    {
        var dist = new Dictionary<string, int>();
        var q = new Queue<string>();
        q.Enqueue(start);
        dist[start] = 0;
        while (q.Count > 0)
        {
            var u = q.Dequeue();
            if (!adj.ContainsKey(u))
                continue;
            foreach (var nb in adj[u])
            {
                if (!dist.ContainsKey(nb))
                {
                    dist[nb] = dist[u] + 1;
                    q.Enqueue(nb);
                }
            }
        }
        return dist;
    }

    static string StateKey(string virus, HashSet<string> edges)
    {
        var sorted = edges.OrderBy(s => s, StringComparer.Ordinal);
        return virus + "|" + string.Join(";", sorted);
    }

    static void Main()
    {
        var edges = new List<(string, string)>();
        string line;

        while ((line = Console.ReadLine()) != null)
        {
            line = line.Trim();
            if (!string.IsNullOrEmpty(line))
            {
                var parts = line.Split('-');
                if (parts.Length == 2)
                {
                    edges.Add((parts[0], parts[1]));
                }
            }
        }

        var result = Solve(edges);
        foreach (var edge in result)
        {
            Console.WriteLine(edge);
        }
    }
}
