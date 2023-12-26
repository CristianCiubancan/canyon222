﻿using System.Drawing;
using System.Text;

namespace Canyon.World.Map
{
    public sealed class TerrainObject : MapObject
    {
        public List<TerrainObjectPart> Parts = new();

        private TerrainObject()
        {
        }

        public uint OwnerIdentity { get; private set; }

        public static TerrainObject CreateNew(BinaryReader scenery, uint idOwner = 0)
        {
            try
            {
                var objTerrain = new TerrainObject
                {
                    OwnerIdentity = idOwner
                };

                int amount = scenery.ReadInt32();
                for (var parts = 0; parts < amount; parts++)
                {
                    TerrainObjectPart terrainPart = new()
                    {
                        AniFile = Encoding.ASCII.GetString(scenery.ReadBytes(256)).Replace('\0', ' ').TrimEnd(),
                        AniTitle = Encoding.ASCII.GetString(scenery.ReadBytes(64)).Replace('\0', ' ').TrimEnd(),

                        PosOffsetX = scenery.ReadInt32(),
                        PosOffsetY = scenery.ReadInt32(),
                        FrameInterval = scenery.ReadInt32(),

                        SizeBaseCX = scenery.ReadInt32(),
                        SizeBaseCY = scenery.ReadInt32(),
                        Thickness = scenery.ReadInt32(),

                        PosSceneOffsetX = scenery.ReadInt32(),
                        PosSceneOffsetY = scenery.ReadInt32(),
                        Height = scenery.ReadInt32()
                    };

                    for (var y = 0; y < terrainPart.SizeBaseCY; y++)
                    {
                        for (var x = 0; x < terrainPart.SizeBaseCX; x++)
                        {
                            int mask = (ushort)scenery.ReadInt32();
                            int terrain = (ushort)scenery.ReadInt32();
                            int altitude = (ushort)scenery.ReadInt32();

                            var layer = new Layer
                            {
                                Altitude = (ushort)altitude,
                                Mask = (ushort)mask,
                                Terrain = (ushort)terrain
                            };
                            terrainPart.Layers.Add(layer);
                        }
                    }

                    objTerrain.AddPart(terrainPart);
                }

                return objTerrain;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public void AddPart(TerrainObjectPart part)
        {
            Parts.Add(part);
        }

        public void RemovePart(int identity)
        {
            Parts.RemoveAll(x => x.Identity == identity);
        }

        public void SetPos(Point pos)
        {
            X = pos.X;
            Y = pos.Y;

            foreach (TerrainObjectPart part in Parts)
            {
                int x = pos.X;
                int y = pos.Y;

                x += part.PosSceneOffsetX;
                y += part.PosSceneOffsetY;

                part.X = x;
                part.Y = y;
            }
        }
    }
}
