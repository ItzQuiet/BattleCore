﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Devastation
{
    public class BaseLoader
    {
        // Holds tileID using a multi dimensional array m_MapGrid[xCoor][yCoor]
        private byte[][] m_MapTileIDs;
        // Holds tileType
        private byte[][] m_MapTileType;

        // 4 different points to check - x and y
        private int[] add_x = { -1, 0, 1, 0 };
        private int[] add_y = { 0, -1, 0, 1 };

        // Load variables in
        public BaseLoader(byte[][] MapTileIDs, byte[][] MapTileType)
        {
            this.m_MapTileIDs = MapTileIDs;
            this.m_MapTileType = MapTileType;
        }

        // All saved safe that were filled
        private List<ushort[]> SafeFills = new List<ushort[]>();

        public void LoadBases(List<Base> BaseList, Base Lobby)
        {
            // Start scanning map
            for (int y = 0; y < 1024; y += 1)
            {
                for (int x = 0; x < 1024; x += 1)
                {
                    if (m_MapTileIDs[x][y] == 171)
                    {
                        // Start to store the dimensions by using first point
                        ushort[] fill_dim = new ushort[] { (ushort)x, (ushort)y, (ushort)(x + 1), (ushort)(y + 1) };

                        // This will hold our fill list
                        Queue<Point> fillQ = new Queue<Point>();
                        // add our first safe tile to list
                        fillQ.Enqueue(new Point(x, y));
                        // erase tile from map
                        m_MapTileIDs[x][y] = 0;

                        // run the fill loop
                        while (fillQ.Count > 0)
                        {
                            // pull the next point out of list
                            Point nextQ = fillQ.Dequeue();

                            // update safe dimensions
                            m_UpdateDimension(fill_dim, nextQ);

                            // 4 point check for fill
                            for (int i = 0; i < 4; i++)
                            {
                                // assign next point to check
                                Point fourPoint = new Point(nextQ.X + add_x[i], nextQ.Y + add_y[i]);

                                // Make sure its a safe
                                if (m_ValidTile(fourPoint) && m_MapTileIDs[fourPoint.X][fourPoint.Y] == 171)
                                {
                                    // add to q
                                    fillQ.Enqueue(fourPoint);
                                    // erase tile to avoid including it in q more than once
                                    m_MapTileIDs[fourPoint.X][fourPoint.Y] = 0;
                                }
                            }
                        }
                        // add safe into our list
                        SafeFills.Add(fill_dim);
                    }
                }
            }

            // Fill the space starting at where the safe was located
            while (SafeFills.Count > 0)
            {
                // Create a base and start saving info
                Base NewBase = new Base();
                NewBase.AlphaSafe = SafeFills[0];

                // Store values
                int x = SafeFills[0][0];
                int y = SafeFills[0][1];

                // Remove it from list
                SafeFills.RemoveAt(0);

                // This will hold our fill list
                Queue<Point> fillQ = new Queue<Point>();

                // Start to store the dimensions by using first point
                ushort[] fill_dim = new ushort[] { (ushort)x, (ushort)y, (ushort)(x + 1), (ushort)(y + 1) };

                // add our first safe tile to list
                fillQ.Enqueue(new Point(x, y));

                int safes_found = 1;

                // run the fill loop
                while (fillQ.Count > 0)
                {
                    // Tile count for safe
                    NewBase.TileCount++;

                    // pull the next point out of list
                    Point nextQ = fillQ.Dequeue();

                    // update safe dimensions
                    m_UpdateDimension(fill_dim, nextQ);

                    // 4 point check for fill
                    for (int i = 0; i < 4; i++)
                    {
                        // assign next point to check
                        Point fourPoint = new Point(nextQ.X + add_x[i], nextQ.Y + add_y[i]);

                        // Tile is "passable"
                        if (m_ValidTile(fourPoint) && m_MapTileType[fourPoint.X][fourPoint.Y] == 1)
                        {
                            // add to q
                            fillQ.Enqueue(fourPoint);
                            // erase tile to avoid including it in q more than once
                            m_MapTileType[fourPoint.X][fourPoint.Y] = 0;

                            // Counting safes found as we fill base
                            // Checking safe list in reverse order as we will be removing some
                            for (int s = SafeFills.Count - 1; s >= 0; s--)
                            {
                                // Coords match
                                if (SafeFills[s][0] == fourPoint.X && SafeFills[s][1] == fourPoint.Y)
                                {
                                    // Add safe to base info
                                    NewBase.BravoSafe = SafeFills[s];
                                    NewBase.BaseDimension = fill_dim;
                                    // Remove the safe found in base
                                    SafeFills.RemoveAt(s);
                                    // Add to our count
                                    safes_found++;
                                }
                            }
                        }
                    }
                }
                // 2 safes make it a valid base
                if (safes_found == 2)
                {
                    BaseList.Add(NewBase);
                    BaseList[BaseList.IndexOf(NewBase)].Number = BaseList.IndexOf(NewBase);
                }
                if (safes_found == 1)
                {
                    // Assign lobby values
                    Lobby.AlphaSafe = NewBase.AlphaSafe;
                    Lobby.AlphaStartX = (ushort)Math.Floor((decimal)((NewBase.AlphaSafe[0] + NewBase.AlphaSafe[2]) / 2));
                    Lobby.AlphaStartY = (ushort)Math.Floor((decimal)((NewBase.AlphaSafe[1] + NewBase.AlphaSafe[3]) / 2));
                    Lobby.BaseDimension = new ushort[] { (ushort)(fill_dim[0] * 16), (ushort)(fill_dim[1] * 16), (ushort)(fill_dim[2] * 16), (ushort)(fill_dim[3] * 16) };
                    Lobby.TileCount = NewBase.TileCount;
                }
            }
            // Grab high and low values from all loaded bases
            int low = BaseList.ElementAt(0).TileCount;
            int high = BaseList.ElementAt(0).TileCount;

            for (int i = 0; i < BaseList.Count; i++)
            {
                if (BaseList[i].TileCount > high)
                    high = BaseList[i].TileCount;
                else if (BaseList[i].TileCount < low)
                    low = BaseList[i].TileCount;
            }
            // divide by 3 to get small/medium/large
            int tier = (int)Math.Floor((decimal)((high - low) / 3));

            for (int i = 0; i < BaseList.Count; i++)
            {
                if (BaseList[i].TileCount > low + (tier * 2))
                    BaseList[i].Size = BaseSize.Large;
                else if (BaseList[i].TileCount > low + tier)
                    BaseList[i].Size = BaseSize.Medium;
                else
                    BaseList[i].Size = BaseSize.Small;

                // Assign safe start points while we loop through bases - to be save in tiles not pixels
                BaseList[i].AlphaStartX = (ushort)Math.Floor((decimal)((BaseList[i].AlphaSafe[0] + BaseList[i].AlphaSafe[2]) / 2));
                BaseList[i].AlphaStartY = (ushort)Math.Floor((decimal)((BaseList[i].AlphaSafe[1] + BaseList[i].AlphaSafe[3]) / 2));
                BaseList[i].BravoStartX = (ushort)Math.Floor((decimal)((BaseList[i].BravoSafe[0] + BaseList[i].BravoSafe[2]) / 2));
                BaseList[i].BravoStartY = (ushort)Math.Floor((decimal)((BaseList[i].BravoSafe[1] + BaseList[i].BravoSafe[3]) / 2));

                // Convert dimension from tile to pixels since we will be 
                // checking player position agains it using pixel position
                ushort[] m_alphaSafe = BaseList[i].AlphaSafe;
                ushort[] m_bravoSafe = BaseList[i].BravoSafe;
                ushort[] baseDimensions = BaseList[i].BaseDimension;
                BaseList[i].AlphaSafe = new ushort[] { (ushort)(m_alphaSafe[0] * 16), (ushort)(m_alphaSafe[1] * 16), (ushort)(m_alphaSafe[2] * 16), (ushort)(m_alphaSafe[3] * 16) };
                BaseList[i].BravoSafe = new ushort[] { (ushort)(m_bravoSafe[0] * 16), (ushort)(m_bravoSafe[1] * 16), (ushort)(m_bravoSafe[2] * 16), (ushort)(m_bravoSafe[3] * 16) };
                BaseList[i].BaseDimension = new ushort[] { (ushort)(baseDimensions[0] * 16), (ushort)(baseDimensions[1] * 16), (ushort)(baseDimensions[2] * 16), (ushort)(baseDimensions[3] * 16) };
            }
        }

        // Checks to see if tiles are on the edge of the fill
        private void m_UpdateDimension(ushort[] dim, Point p)
        {
            // x-coord
            if (p.X < dim[0]) dim[0] = (ushort)p.X;
            else if (p.X + 1 > dim[2]) dim[2] = (ushort)(p.X + 1);
            // y-coord
            if (p.Y < dim[1]) dim[1] = (ushort)p.Y;
            else if (p.Y + 1 > dim[3]) dim[3] = (ushort)(p.Y + 1);
        }

        // makes sure tile being checked isnt out of bounds for map
        private bool m_ValidTile(Point p)
        {
            if (p.X >= 0 && p.X < 1024 && p.Y >= 0 && p.Y < 1024)
                return true;
            return false;
        }
    }
}
