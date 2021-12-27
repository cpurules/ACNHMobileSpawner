﻿namespace NHSE.Core
{
    /// <summary>
    /// Converts Villager House objects to different revisions.
    /// </summary>
    public static class VillagerHouseConverter
    {
        /// <summary>
        /// Checks to see if the <see cref="size"/> matches any of the House object sizes.
        /// </summary>
        /// <param name="size">Size of the byte array that might represent a House object.</param>
        /// <returns>True if it matches any size.</returns>
        public static bool IsHouse(int size)
        {
            return size == VillagerHouse1.SIZE || size == VillagerHouse2.SIZE;
        }

        /// <summary>
        /// Checks to see if the input <see cref="size"/> can be converted to the <see cref="expect"/> size.
        /// </summary>
        /// <param name="size">Current house file size</param>
        /// <param name="expect">Expected house file size</param>
        /// <returns>True if can be converted, false if no conversion available.</returns>
        public static bool IsCompatible(int size, int expect)
        {
            if (expect == VillagerHouse1.SIZE || expect == VillagerHouse2.SIZE)
                return IsHouse(size);

            return false;
        }

        /// <summary>
        /// Converts the House data format to another format.
        /// </summary>
        /// <remarks>
        /// Before calling this method, check that a conversion method exists via <see cref="IsCompatible"/> and that the length of the <see cref="data"/> is not the same as what you <see cref="expect"/>.
        /// If the sizes are the same, it will return the input <see cref="data"/>.
        /// If no conversion path exists, it will return the input <see cref="data"/>.
        /// </remarks>
        /// <param name="data">Current format</param>
        /// <param name="expect">Target size</param>
        /// <returns>Converted data</returns>
        public static byte[] GetCompatible(byte[] data, int expect)
        {
            if (data.Length == expect)
                return data;

            if (expect == VillagerHouse1.SIZE && data.Length == VillagerHouse2.SIZE)
                return Convert21(data);

            if (expect == VillagerHouse2.SIZE && data.Length == VillagerHouse1.SIZE)
                return Convert12(data);

            return data;
        }

        /// <summary>
        /// Converts a <see cref="VillagerHouse1"/> object byte array to a <see cref="VillagerHouse2"/>
        /// </summary>
        /// <param name="h1"><see cref="VillagerHouse1"/> object byte array</param>
        /// <returns><see cref="VillagerHouse2"/> object byte array</returns>
        private static byte[] Convert12(byte[] h1) => new VillagerHouse1(h1).Upgrade().Data;

        /// <summary>
        /// Converts a <see cref="VillagerHouse2"/> object byte array to a <see cref="VillagerHouse1"/>
        /// </summary>
        /// <param name="h2"><see cref="VillagerHouse2"/> object byte array</param>
        /// <returns><see cref="VillagerHouse1"/> object byte array</returns>
        private static byte[] Convert21(byte[] h2) => new VillagerHouse2(h2).Downgrade().Data;
    }
}
