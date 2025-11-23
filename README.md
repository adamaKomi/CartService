# Documentation Technique - CartService

## Vue d'ensemble

CartService est une API RESTful développée en ASP.NET Core 9 pour la gestion des paniers d'achat. L'application utilise Redis comme système de cache distribué pour stocker les données des paniers utilisateurs de manière temporaire et performante.

## Architecture

L'application suit une architecture minimal API avec des endpoints définis de manière déclarative. Les données sont stockées dans Redis sous forme de chaînes JSON sérialisées.

### Composants principaux

- **Program.cs** : Point d'entrée de l'application, configuration des services et du pipeline HTTP
- **Endpoints/CartEndpoints.cs** : Définition de tous les endpoints de l'API
- **Models/CartItem.cs** : Modèle de données pour les articles du panier
- **Redis** : Cache distribué pour la persistance des paniers

## Technologies utilisées

- **.NET 9** : Framework de développement
- **ASP.NET Core Minimal API** : Pour la création d'APIs REST
- **StackExchange.Redis** : Client Redis pour .NET
- **DotNetEnv** : Chargement des variables d'environnement depuis un fichier .env
- **Scalar.AspNetCore** : Génération de documentation OpenAPI interactive
- **C# 13** : Version du langage

## Installation et configuration

### Prérequis

- .NET 9 SDK
- Redis Server (local ou distant)
- IDE (Visual Studio, VS Code, etc.)

### Installation

1. Cloner le repository
2. Restaurer les packages NuGet :
   ```bash
   dotnet restore
   ```
3. Configurer les variables d'environnement

### Configuration

Créer un fichier `.env` à la racine du projet :

```
REDIS_CONNECTION_STRING=localhost:6379,password=,ssl=False,abortConnect=False
```

Pour Redis distant (Azure Cache for Redis) :
```
REDIS_CONNECTION_STRING=your-redis-server.redis.cache.windows.net:6380,password=your-access-key,ssl=True,abortConnect=False
```

### Lancement

```bash
dotnet run
```

L'application démarre par défaut sur `http://localhost:5000` (ou le port configuré).

## Endpoints API

### Base URL
`https://cartservice.wittydune-edc9f6e7.francecentral.azurecontainerapps.io` (production)

En développement : `http://localhost:5000`

### 1. Health Check
- **Endpoint** : `GET /health`
- **Description** : Vérifie que le service fonctionne
- **Réponse** : `CartService is running...`

### 2. Récupérer le panier d'un utilisateur
- **Endpoint** : `GET /api/cart/{userId}`
- **Description** : Récupère tous les articles du panier avec le total
- **Paramètres** :
  - `userId` (string) : Identifiant de l'utilisateur
- **Réponse** :
  ```json
  {
    "cart": [
      {
        "productId": "P01",
        "productName": "Clavier mécanique",
        "quantity": 1,
        "price": 350.0
      }
    ],
    "total": 350.0
  }
  ```
- **Erreurs** : 404 si panier non trouvé

### 3. Récupérer un article spécifique
- **Endpoint** : `GET /api/cart/{userId}/{productId}`
- **Description** : Récupère un article spécifique du panier
- **Paramètres** :
  - `userId` (string)
  - `productId` (string)
- **Réponse** : Objet CartItem
- **Erreurs** : 404 si panier ou produit non trouvé

### 4. Ajouter un article au panier
- **Endpoint** : `POST /api/cart/{userId}`
- **Description** : Ajoute un nouvel article au panier
- **Paramètres** :
  - `userId` (string)
- **Corps de la requête** :
  ```json
  {
    "productId": "P01",
    "productName": "Clavier mécanique",
    "quantity": 1,
    "price": 350.0
  }
  ```
- **Réponse** : L'article ajouté
- **Erreurs** : 400 si quantité invalide ou produit déjà existant

### 5. Supprimer un article du panier
- **Endpoint** : `DELETE /api/cart/{userId}/{productId}`
- **Description** : Supprime un article spécifique du panier
- **Paramètres** :
  - `userId` (string)
  - `productId` (string)
- **Réponse** : Message de confirmation
- **Erreurs** : 404 si panier ou produit non trouvé

### 6. Vider le panier
- **Endpoint** : `DELETE /api/cart/{userId}`
- **Description** : Supprime complètement le panier d'un utilisateur
- **Paramètres** :
  - `userId` (string)
- **Réponse** : Message de confirmation

### 7. Modifier la quantité d'un article
- **Endpoint** : `PUT /api/cart/{userId}/{productId}/{action}/{quantity?}`
- **Description** : Modifie la quantité d'un article
- **Paramètres** :
  - `userId` (string)
  - `productId` (string)
  - `action` (string) : "increase", "decrease", ou "new-quantity"
  - `quantity` (int, optionnel) : Nouvelle quantité pour "new-quantity"
- **Exemples** :
  - `PUT /api/cart/user1/P01/increase` : Augmente de 1
  - `PUT /api/cart/user1/P01/decrease` : Diminue de 1 (minimum 1)
  - `PUT /api/cart/user1/P01/new-quantity/5` : Définit à 5
- **Réponse** :
  ```json
  {
    "message": "Quantity updated successfully",
    "product": { ... }
  }
  ```
- **Erreurs** : 400 pour action invalide, 404 si panier/produit non trouvé

## Modèles de données

### CartItem
```csharp
public class CartItem
{
    public string ProductId { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

## Gestion des erreurs

L'API retourne des codes HTTP standard :
- `200` : Succès
- `400` : Requête invalide
- `404` : Ressource non trouvée
- `500` : Erreur serveur (problème Redis)

## Tests avec Postman

### Configuration de la collection

1. Importer la collection `tests/Production.postman_collection.json`
2. La collection est pré-configurée pour l'environnement de production avec l'URL : `https://cartservice.wittydune-edc9f6e7.francecentral.azurecontainerapps.io`
3. Pour les tests en développement local, modifier la variable `baseURL` vers `http://localhost:5000`
4. Définir les autres variables :
   - `userId` : `user1` (ou tout identifiant utilisateur)
   - `productId` : `P01` (ou ID de produit)
   - `newQuantity` : `5`

### Scénario de test complet

1. **Vérifier la santé** : `GET /health`
2. **Ajouter un article** : `POST /api/cart/{{userId}}` avec corps JSON
3. **Récupérer le panier** : `GET /api/cart/{{userId}}`
4. **Récupérer un article** : `GET /api/cart/{{userId}}/{{productId}}`
5. **Modifier la quantité** : `PUT /api/cart/{{userId}}/{{productId}}/increase`
6. **Supprimer un article** : `DELETE /api/cart/{{userId}}/{{productId}}`
7. **Vider le panier** : `DELETE /api/cart/{{userId}}`

### Exemples de requêtes

#### Ajouter un produit
```json
{
  "productId": "P01",
  "productName": "Clavier mécanique",
  "quantity": 2,
  "price": 350.00
}
```

#### Réponse typique du panier
```json
{
  "cart": [
    {
      "productId": "P01",
      "productName": "Clavier mécanique",
      "quantity": 2,
      "price": 350.00
    }
  ],
  "total": 700.00
}
```

## Déploiement

### Développement local
- Lancer Redis localement
- `dotnet run`

### Production
- Configurer Redis distant (Azure Cache for Redis recommandé)
- Déployer sur Azure Container Apps ou App Service
- Utiliser des variables d'environnement pour la configuration

### Docker
Un Dockerfile est fourni pour la containerisation.

```bash
docker build -t cartservice .
docker run -p 5000:80 cartservice
```

## Sécurité

- Les connexions Redis peuvent être sécurisées via SSL
- Utiliser des variables d'environnement pour les secrets
- En production, envisager l'utilisation d'Azure Key Vault pour les clés Redis

## Monitoring

- Endpoint `/health` pour les checks de santé
- Logs intégrés d'ASP.NET Core
- Métriques Redis disponibles via les outils Redis

## Contribution

1. Fork le repository
2. Créer une branche feature
3. Commiter les changements
4. Push et créer une PR

## Support

Pour les problèmes ou questions, créer une issue dans le repository.