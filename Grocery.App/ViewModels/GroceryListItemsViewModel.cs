using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;

namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = [];
        public ObservableCollection<Product> AvailableProducts { get; set; } = [];

        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);

        public GroceryListItemsViewModel(IGroceryListItemsService groceryListItemsService, IProductService productService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            Load(groceryList.Id);
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id)) MyGroceryListItems.Add(item);
            GetAvailableProducts();
        }

        private void GetAvailableProducts()
        {
            // Maak de lijst AvailableProducts leeg
            AvailableProducts.Clear();

            // Haal de lijst met producten op
            var allProducts = _productService.GetAll();

            // Haal items op de huidige boodschappenlijst op
            var itemsOnList = _groceryListItemsService.GetAllOnGroceryListId(GroceryList.Id);
            var idsOnList = itemsOnList.Select(i => i.ProductId).ToHashSet();

            // Controleer: niet al op de boodschappenlijst en voorraad > 0
            foreach (var p in allProducts)
            {
                if (p.Id > 0 && p.Stock > 0 && !idsOnList.Contains(p.Id))
                {
                    AvailableProducts.Add(p);
                }
            }
        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            Load(value.Id);
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> paramater = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, paramater);
        }
        [RelayCommand]
        public void AddProduct(Product product)
        {
            // Controleer of het product bestaat en dat de Id > 0
            if (product is null || product.Id <= 0)
                return;

            // Maak een GroceryListItem met Id 0 en vul de juiste productid en grocerylistid
            var newItem = new GroceryListItem(
                id: 0,
                groceryListId: GroceryList.Id,
                productId: product.Id,
                amount: 1
            );

            // Voeg het GroceryListItem toe aan de dataset middels de _groceryListItemsService
            _groceryListItemsService.Add(newItem);

            // Werk de voorraad (Stock) van het product bij en zorg dat deze wordt vastgelegd (middels _productService)
            if (product.Stock > 0)
            {
                product.Stock -= 1;
                _productService.Update(product);
            }

            // Werk de lijst AvailableProducts bij, want dit product is niet meer beschikbaar
            // (niet meer beschikbaar = staat nu op de boodschappenlijst, ongeacht resterende stock)
            var toRemove = AvailableProducts.FirstOrDefault(p => p.Id == product.Id);
            if (toRemove is not null)
                AvailableProducts.Remove(toRemove);

            // call OnGroceryListChanged(GroceryList);
            OnGroceryListChanged(GroceryList);
        }
    }
}
