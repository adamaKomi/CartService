using System.Text.Json;
using CartService.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace CartService.Endpoints
{
    // Classe statique contenant les définitions des endpoints pour le service de panier
    public static class CartEndpoints
    {
        // Méthode d’extension qui enregistre tous les endpoints dans l’application
        public static void MapCartEndpoints(this WebApplication app)
        {
            // Endpoint de test pour vérifier si le service fonctionne
            app.MapGet("/health", () => "CartService is running...");





            app.MapGet("/api/cart/{userId}", async (string userId, IDistributedCache cache) =>
            {
                try
                {

                    // Lire la chaîne depuis Redis
                    var data = await cache.GetStringAsync(userId);

                    // Pas de données
                    if (string.IsNullOrWhiteSpace(data))
                        return Results.NotFound($"Cart not found for {userId}");


                    // Désérialiser  - si null, on crée une liste vide
                    var cart = JsonSerializer.Deserialize<List<CartItem>>(data) ?? new List<CartItem>();

                    // Calculer le total
                    decimal total = 0m;
                    if (cart.Any())
                    {

                        total = cart.Sum(p => (p?.Price ?? 0m) * (p?.Quantity ?? 0));
                    }

                    return Results.Ok(new
                    {
                        cart = cart,
                        total = total
                    });
                
                }
                catch(Exception ex)
                {
                    return Results.Problem($"Redis error: {ex.Message}");
                }
            });






            // Récupérer un produit spécifique dans le panier d’un utilisateur
            app.MapGet("/api/cart/{userId}/{productId}", async (string userId, string productId, IDistributedCache cache) =>
            {
                var data = await cache.GetStringAsync(userId);
                if (data is null) return Results.NotFound("Cart not found");

                var cart = JsonSerializer.Deserialize<List<CartItem>>(data);

                // Recherche du produit correspondant dans le panier
                var product = cart?.FirstOrDefault(p => p.ProductId == productId);

                if (product is null) return Results.NotFound($"Product {productId} not found in the cart.");

                return Results.Ok(product);
            });





            // Ajouter un nouvel article au panier d’un utilisateur
            app.MapPost("/api/cart/{userId}", async (string userId, CartItem item, IDistributedCache cache) =>
            {
                // Vérifie que la quantité est positive
                if (item.Quantity <= 0) return Results.BadRequest("The quantity must be positive");

                // Récupère les données actuelles du panier dans Redis
                var data = await cache.GetStringAsync(userId);
                List<CartItem> cart;

                try
                {
                    // Si le panier n’existe pas encore → crée une nouvelle liste
                    cart = data is null
                        ? new List<CartItem>()
                        : JsonSerializer.Deserialize<List<CartItem>>(data)!;
                }
                catch
                {
                    // Si la désérialisation échoue → problème de format JSON
                    return Results.Problem("Fail to parse cart data");
                }

                // Vérifie si le produit existe déjà dans le panier
                var product = cart.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (product is not null)
                    return Results.BadRequest("This product already exists!");

                // Ajoute le nouvel article au panier
                cart.Add(item);

                // Sauvegarde du panier mis à jour dans Redis
                await cache.SetStringAsync(userId, JsonSerializer.Serialize(cart));

                // Retourne uniquement le produit ajouté (confirmation)
                return Results.Ok(
                    JsonSerializer.Deserialize<List<CartItem>>(
                        await cache.GetStringAsync(userId)
                        )?
                    .FirstOrDefault(p => p.ProductId == item.ProductId));
            });





            // Supprimer un article spécifique du panier
            app.MapDelete("/api/cart/{userId}/{productId}", async (string userId, string productId, IDistributedCache cache) =>
            {
                var data = await cache.GetStringAsync(userId);
                if (data is null) return Results.NotFound("Cart not found.");

                // Désérialisation, suppression du produit correspondant
                var cart = JsonSerializer.Deserialize<List<CartItem>>(data)!;
                // Chercher le produit
                var product = cart.FirstOrDefault(p => p.ProductId == productId);
                // Vérifier l'existance du produit
                if (product is null) return Results.NotFound($"This product {productId} doesn't exist");
                // Modifier à jour le panier
                cart = cart.Where(x => x.ProductId != productId).ToList();

                // Mise à jour du cache Redis avec le panier modifié
                await cache.SetStringAsync(userId, JsonSerializer.Serialize(cart));

                // Retourne le panier après suppression
                return Results.Ok($"The product with productId = {productId} has been deleted successfully!");
            });





            // Supprimer complètement le panier d’un utilisateur
            app.MapDelete("/api/cart/{userId}", async (string userId, IDistributedCache cache) =>
            {
                // Supprime la clé correspondante dans Redis
                await cache.RemoveAsync(userId);
                return Results.Ok($"Cart for user {userId} cleared.");
            });





            // Mettre à jour la quantité d’un produit existant dans le panier
            app.MapPut("/api/cart/{userId}/{productId}/{action}/{quantity?}", async (
                string userId,
                string productId,
                string action,
                int? quantity,
                IDistributedCache cache) =>
            {
                // Vérification des actions valides
                if (action is not ("increase" or "decrease" or "new-quantity"))
                    return Results.BadRequest("Invalid action. Use increase, decrease, add, or substract.");

                // Vérifie la validité de la quantité pour certaines actions
                if ((action is "new-quantity") && (!quantity.HasValue || quantity <= 0))
                    return Results.BadRequest("Quantity must be positive for new-quantity actions.");

                // Récupération du panier depuis Redis
                var data = await cache.GetStringAsync(userId);
                if (data is null)
                    return Results.NotFound($"Cart not found for {userId}");

                var cart = JsonSerializer.Deserialize<List<CartItem>>(data);
                var product = cart?.FirstOrDefault(p => p.ProductId == productId);

                if (product is null)
                    return Results.NotFound($"Product {productId} not found in cart.");

                // Application de l’action
                switch (action)
                {
                    case "increase":
                        product.Quantity++;
                        break;

                    case "decrease":
                        if (product.Quantity > 1)
                            product.Quantity--;
                        else
                            return Results.BadRequest("Cannot decrease below 1.");
                        break;

                    case "new-quantity":
                        product.Quantity = quantity!.Value;
                        break;

                    default:
                        return Results.BadRequest("Action incorrect");
                }

                // Sauvegarde du panier mis à jour
                await cache.SetStringAsync(userId, JsonSerializer.Serialize(cart));

                return Results.Ok(new
                {
                    Message = "Quantity updated successfully",
                    Product = product
                });
            });
        }
    }
}
