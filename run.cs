#!/usr/bin/env -S dotnet-script

using System;
using System.Collections.Generic;

class Program
{
    static readonly int[] EnergyPerType = { 1, 10, 100, 1000 };
    static readonly int[] RoomEntrances = { 2, 4, 6, 8 };
    static readonly int[] HallwayStops = { 0, 1, 3, 5, 7, 9, 10 };

    static (char[], char[]) ParseInput(List<string> lines)
    {
        if (lines == null || lines.Count == 0)
            throw new ArgumentException("Нет строк");

        var depth = lines.Count - 3;
        if (depth <= 0)
            throw new ArgumentException("Глубина комнат <= 0");

        var hallwayChars = new char[11];
        var hallwayLine = lines[1];
        if (hallwayLine.Length < 12)
            throw new ArgumentException("Неправильная строка коридора");

        for (var i = 0; i < 11; i++)
        {
            hallwayChars[i] = hallwayLine[1 + i];
        }

        var rooms = new char[4 * depth];
        for (var r = 0; r < 4; r++)
        {
            for (var d = 0; d < depth; d++)
            {
                var lineIdx = 2 + d;
                var line = lines[lineIdx];
                var idx = 3 + r * 2;
                if (idx >= line.Length)
                    rooms[r * depth + d] = '.';
                else
                    rooms[r * depth + d] = line[idx];
            }
        }

        return (hallwayChars, rooms);
    }

    static string GetGoal(int depth)
    {
        var goalRooms = new char[4 * depth];
        for (var i = 0; i < 4; i++)
            for (var d = 0; d < depth; d++)
                goalRooms[i * depth + d] = (char)('A' + i);

        var goal = new string('.', 11) + new string(goalRooms);
        return goal;
    }

    static bool IsPathClearHallway(char[] hall, int from, int to)
    {
        if (from == to) return hall[to] == '.';
        var dir = to > from ? 1 : -1;
        for (var p = from + dir; p != to + dir; p += dir)
            if (hall[p] != '.') return false;
        return true;
    }

    static bool IsPathClearFromEntranceToStop(char[] hall, int entrance, int stop)
    {
        if (hall[entrance] != '.') return false;
        if (entrance == stop) return true;
        var dir = stop > entrance ? 1 : -1;
        for (var p = entrance + dir; p != stop + dir; p += dir)
            if (hall[p] != '.') return false;
        return true;
    }

    static long Solve(List<string> lines)
    {
        var (hallwayChars, rooms) = ParseInput(lines);
        var depth = lines.Count - 3;

        var start = new string(hallwayChars) + new string(rooms);
        var goal = GetGoal(depth);

        var dist = new Dictionary<string, long>();
        var pq = new PriorityQueue<string, long>();

        dist[start] = 0;
        pq.Enqueue(start, 0);

        while (pq.TryDequeue(out var state, out long cost))
        {
            if (cost != dist[state]) continue;
            if (state == goal) return cost;

            var hall = state[..11].ToCharArray();
            var roomChars = state[11..].ToCharArray();

            for (var h = 0; h < 11; h++)
            {
                var sym = hall[h];
                if (sym == '.') continue;


                var t = sym - 'A';

                var isRoomOk = true;
                for (var d = 0; d < depth; d++)
                {
                    var rc = roomChars[t * depth + d];
                    if (rc != '.' && rc != sym)
                    {
                        isRoomOk = false;
                        break;
                    }
                }
                if (!isRoomOk) continue;

                var ent = RoomEntrances[t];

                if (!IsPathClearHallway(hall, h, ent)) continue;

                var td = -1;
                for (var d = depth - 1; d >= 0; d--)
                {
                    if (roomChars[t * depth + d] == '.')
                    {
                        td = d;
                        break;
                    }
                }
                if (td == -1) continue;

                long steps = Math.Abs(h - ent) + td + 1;
                long moveCost = steps * EnergyPerType[t];

                var nh = (char[])hall.Clone();
                var nr = (char[])roomChars.Clone();
                nh[h] = '.';
                nr[t * depth + td] = sym;
                var ns = new string(nh) + new string(nr);
                long nc = cost + moveCost;

                if (!dist.ContainsKey(ns) || nc < dist[ns])
                {
                    dist[ns] = nc;
                    pq.Enqueue(ns, nc);
                }
            }

            for (var r = 0; r < 4; r++)
            {
                var roomNeedsMove = false;
                for (var d = 0; d < depth; d++)
                {
                    var c = roomChars[r * depth + d];
                    if (c != '.' && c != (char)('A' + r))
                    {
                        roomNeedsMove = true;
                        break;
                    }
                }
                if (!roomNeedsMove) continue;

                var top = -1;
                for (var d = 0; d < depth; d++)
                {
                    if (roomChars[r * depth + d] != '.')
                    {
                        top = d;
                        break;
                    }
                }
                if (top == -1) continue;

                var amph = roomChars[r * depth + top];
                var ent = RoomEntrances[r];

                foreach (var hpos in HallwayStops)
                {
                    if (hall[hpos] != '.') continue;
                    if (!IsPathClearFromEntranceToStop(hall, ent, hpos)) continue;

                    long steps = Math.Abs(hpos - ent) + top + 1;
                    long moveCost = steps * EnergyPerType[amph - 'A'];

                    var nh = (char[])hall.Clone();
                    var nr = (char[])roomChars.Clone();
                    nh[hpos] = amph;
                    nr[r * depth + top] = '.';
                    var ns = new string(nh) + new string(nr);
                    long nc = cost + moveCost;

                    if (!dist.ContainsKey(ns) || nc < dist[ns])
                    {
                        dist[ns] = nc;
                        pq.Enqueue(ns, nc);
                    }
                }
            }
        }

        return long.MaxValue;
    }


    static void Main()
    {
        var lines = new List<string>();
        string line;

        while ((line = Console.ReadLine()) != null)
        {
            lines.Add(line);
        }

        long result = Solve(lines);
        Console.WriteLine(result);
    }
}
