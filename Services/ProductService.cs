using System;
using Il2CppScheduleOne.Product;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Service for managing product and drug-related operations
    /// </summary>
    public sealed class ProductService
    {
        private static readonly ProductService _instance = new ProductService();
        public static ProductService Instance => _instance;
        private ProductService() { }
        /// <summary>
        /// Gets the current product manager instance
        /// </summary>
        private ProductManager GetProductManager()
        {
            try
            {
                return UnityEngine.Object.FindObjectOfType<ProductManager>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to get ProductManager: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Discovers all available products (Meth, Cocaine, Shrooms)
        /// </summary>
        public void DiscoverAllProducts()
        {
            try
            {
                var productManager = GetProductManager();
                if (productManager == null)
                {
                    Debug.LogError("[Nugzz] ProductManager not found");
                    return;
                }

                // Use the static discovery methods from ProductManager
                productManager.SetMethDiscovered();
                productManager.SetCocaineDiscovered();
                productManager.SetShroomsDiscovered();

                Debug.Log("[Nugzz] Discovered all products: Meth, Cocaine, Shrooms");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to discover all products: {ex.Message}");
            }
        }

        /// <summary>
        /// Discovers all available product strains
        /// </summary>
        public void DiscoverAllStrains()
        {
            try
            {
                var productManager = GetProductManager();
                if (productManager == null)
                {
                    Debug.LogError("[Nugzz] ProductManager not found");
                    return;
                }

                Debug.Log("[Nugzz] Discovering all strains...");
                // Implementation depends on actual ProductManager API in Schedule I
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to discover all strains: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the strain registry count
        /// </summary>
        public int GetStrainCount()
        {
            try
            {
                var productManager = GetProductManager();
                if (productManager == null) return 0;

                // Try to get strain registry if available
                Debug.Log("[Nugzz] Getting strain count...");
                return productManager?.AllProducts?.Count ?? 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to get strain count: {ex.Message}");
                return 0;
            }
        }
    }
}
