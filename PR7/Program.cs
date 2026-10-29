using PR7;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ПР7
{
    class Core
    {
        public static PR7Entities Context = new PR7Entities();
    }

    internal class Program
    {
        static User currentUser = null;

        static string ReadText(string label)
        {
            string text = "";
            while (string.IsNullOrWhiteSpace(text))
            {
                Console.Write(label + ": ");
                text = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine("Поле не может быть пустым");
                }
            }
            return text.Trim();
        }

        static int ReadInt(string label, int min, int max)
        {
            int number = min - 1;
            while (number < min || number > max)
            {
                Console.Write(label + ": ");
                if (!int.TryParse(Console.ReadLine(), out number) || number < min || number > max)
                {
                    Console.WriteLine("Введите число от " + min + " до " + max);
                    number = min - 1;
                }
            }
            return number;
        }

        static void Pause()
        {
            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу...");
            Console.ReadKey(true);
        }

        static List<Product> ShowProducts()
        {
            List<Product> products = Core.Context.Product.OrderBy(p => p.Id).ToList();
            if (products.Count == 0)
            {
                Console.WriteLine("Товаров нет");
                return products;
            }

            for (int i = 0; i < products.Count; i++)
            {
                Product p = products[i];
                Console.WriteLine((i + 1) + ". " + p.Name + " | " + p.Category.Name + " | " + p.Price + " руб. | В наличии: " + p.StockQuantity);
            }
            return products;
        }

        static void Register()
        {
            string login = ReadText("Логин");
            while (Core.Context.User.Any(u => u.Login == login))
            {
                Console.WriteLine("Такой логин уже занят");
                login = ReadText("Логин");
            }

            string email = ReadText("Email");
            while (!email.Contains("@"))
            {
                Console.WriteLine("Email должен содержать @");
                email = ReadText("Email");
            }

            string password = ReadText("Пароль");
            while (password.Length < 6)
            {
                Console.WriteLine("Пароль должен быть не короче 6 символов");
                password = ReadText("Пароль");
            }

            string confirm = ReadText("Повторите пароль");
            while (confirm != password)
            {
                Console.WriteLine("Пароли не совпадают");
                confirm = ReadText("Повторите пароль");
            }

            User user = new User();
            user.Login = login;
            user.Email = email;
            user.Password = password;
            Core.Context.User.Add(user);
            Core.Context.SaveChanges();

            currentUser = user;
            Console.WriteLine("Регистрация прошла успешно. Вы вошли как " + user.Login);
        }

        static void Login()
        {
            string login = ReadText("Логин");
            string password = ReadText("Пароль");

            User user = Core.Context.User.FirstOrDefault(u => u.Login == login && u.Password == password);
            if (user == null)
            {
                Console.WriteLine("Неверный логин или пароль");
            }
            else
            {
                currentUser = user;
                Console.WriteLine("Добро пожаловать, " + user.Login);
            }
        }

        static void AddToCart()
        {
            List<Product> products = ShowProducts();
            if (products.Count == 0)
            {
                return;
            }

            Console.WriteLine();
            int index = ReadInt("Номер товара", 1, products.Count) - 1;
            Product product = products[index];

            int userId = currentUser.Id;
            int productId = product.Id;
            CartItem item = Core.Context.CartItem.FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);

            int alreadyInCart = 0;
            if (item != null)
            {
                alreadyInCart = item.Quantity;
            }

            int maxQuantity = product.StockQuantity - alreadyInCart;
            if (maxQuantity <= 0)
            {
                Console.WriteLine("Этого товара больше нет в наличии");
                return;
            }

            int quantity = ReadInt("Количество", 1, maxQuantity);

            if (item == null)
            {
                item = new CartItem();
                item.UserId = userId;
                item.ProductId = productId;
                item.Quantity = quantity;
                Core.Context.CartItem.Add(item);
            }
            else
            {
                item.Quantity = item.Quantity + quantity;
            }

            Core.Context.SaveChanges();
            Console.WriteLine("Товар добавлен в корзину");
        }

        static List<CartItem> ShowCart()
        {
            int userId = currentUser.Id;
            List<CartItem> cart = Core.Context.CartItem.Where(c => c.UserId == userId).OrderBy(c => c.Id).ToList();

            if (cart.Count == 0)
            {
                Console.WriteLine("Корзина пуста");
                return cart;
            }

            decimal total = 0;
            for (int i = 0; i < cart.Count; i++)
            {
                CartItem item = cart[i];
                decimal sum = item.Product.Price * item.Quantity;
                total = total + sum;
                Console.WriteLine((i + 1) + ". " + item.Product.Name + " | " + item.Quantity + " шт. x " + item.Product.Price + " руб. = " + sum + " руб.");
            }
            Console.WriteLine("Итого: " + total + " руб.");
            return cart;
        }

        static void RemoveFromCart()
        {
            List<CartItem> cart = ShowCart();
            if (cart.Count == 0)
            {
                return;
            }

            Console.WriteLine();
            int index = ReadInt("Номер товара для удаления", 1, cart.Count) - 1;
            Core.Context.CartItem.Remove(cart[index]);
            Core.Context.SaveChanges();
            Console.WriteLine("Товар удалён из корзины");
        }

        static PickupPoint ChoosePickupPoint()
        {
            List<PickupPoint> points = Core.Context.PickupPoint.OrderBy(p => p.Id).ToList();

            Console.WriteLine();
            Console.WriteLine("Пункты выдачи:");
            for (int i = 0; i < points.Count; i++)
            {
                Console.WriteLine((i + 1) + ". " + points[i].Address + " (" + points[i].WorkingHours + ")");
            }

            int index = ReadInt("Номер пункта выдачи", 1, points.Count) - 1;
            return points[index];
        }

        static Order CreateOrder(PickupPoint point)
        {
            Order order = new Order();
            order.UserId = currentUser.Id;
            order.PickupPointId = point.Id;
            order.CreatedAt = DateTime.Now;
            Core.Context.Order.Add(order);
            Core.Context.SaveChanges();
            return order;
        }

        static void MoveToOrder(Order order, CartItem item)
        {
            OrderProduct orderProduct = new OrderProduct();
            orderProduct.OrderId = order.Id;
            orderProduct.ProductId = item.ProductId;
            orderProduct.Quantity = item.Quantity;
            orderProduct.Price = item.Product.Price;
            Core.Context.OrderProduct.Add(orderProduct);

            item.Product.StockQuantity = item.Product.StockQuantity - item.Quantity;
            Core.Context.CartItem.Remove(item);
        }

        static void BuyOne()
        {
            List<CartItem> cart = ShowCart();
            if (cart.Count == 0)
            {
                return;
            }

            Console.WriteLine();
            int index = ReadInt("Номер товара для покупки", 1, cart.Count) - 1;
            CartItem item = cart[index];

            if (item.Quantity > item.Product.StockQuantity)
            {
                Console.WriteLine("На складе недостаточно товара: " + item.Product.Name);
                return;
            }

            PickupPoint point = ChoosePickupPoint();
            Order order = CreateOrder(point);
            MoveToOrder(order, item);
            Core.Context.SaveChanges();

            Console.WriteLine("Заказ №" + order.Id + " оформлен. Пункт выдачи: " + point.Address);
        }

        static void BuyAll()
        {
            List<CartItem> cart = ShowCart();
            if (cart.Count == 0)
            {
                return;
            }

            foreach (CartItem item in cart)
            {
                if (item.Quantity > item.Product.StockQuantity)
                {
                    Console.WriteLine("На складе недостаточно товара: " + item.Product.Name);
                    return;
                }
            }

            PickupPoint point = ChoosePickupPoint();
            Order order = CreateOrder(point);
            foreach (CartItem item in cart)
            {
                MoveToOrder(order, item);
            }
            Core.Context.SaveChanges();

            Console.WriteLine("Заказ №" + order.Id + " оформлен. Пункт выдачи: " + point.Address);
        }

        static void ShowOrders()
        {
            int userId = currentUser.Id;
            List<Order> orders = Core.Context.Order.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToList();

            if (orders.Count == 0)
            {
                Console.WriteLine("У вас пока нет заказов");
                return;
            }

            foreach (Order order in orders)
            {
                Console.WriteLine("Заказ №" + order.Id + " от " + order.CreatedAt.ToString("dd.MM.yyyy HH:mm") + " | ПВЗ: " + order.PickupPoint.Address);

                int orderId = order.Id;
                List<OrderProduct> lines = Core.Context.OrderProduct.Where(op => op.OrderId == orderId).ToList();

                decimal total = 0;
                foreach (OrderProduct line in lines)
                {
                    decimal sum = line.Price * line.Quantity;
                    total = total + sum;
                    Console.WriteLine("   - " + line.Product.Name + " | " + line.Quantity + " шт. x " + line.Price + " руб.");
                }
                Console.WriteLine("   Сумма: " + total + " руб.");
                Console.WriteLine();
            }
        }

        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== GMWOG||GG.MOW||WONGG ===");

                if (currentUser == null)
                {
                    Console.WriteLine("Вы не вошли в аккаунт");
                    Console.WriteLine();
                    Console.WriteLine("1. Каталог товаров");
                    Console.WriteLine("2. Регистрация");
                    Console.WriteLine("3. Вход");
                    Console.WriteLine("0. Выход");
                    Console.WriteLine();
                    int choice = ReadInt("Выберите пункт", 0, 3);
                    Console.WriteLine();

                    if (choice == 0)
                    {
                        return;
                    }
                    else if (choice == 1)
                    {
                        ShowProducts();
                    }
                    else if (choice == 2)
                    {
                        Register();
                    }
                    else if (choice == 3)
                    {
                        Login();
                    }
                }
                else
                {
                    Console.WriteLine("Вы вошли как: " + currentUser.Login);
                    Console.WriteLine();
                    Console.WriteLine("1. Каталог товаров");
                    Console.WriteLine("2. Добавить товар в корзину");
                    Console.WriteLine("3. Корзина");
                    Console.WriteLine("4. Удалить товар из корзины");
                    Console.WriteLine("5. Купить один товар");
                    Console.WriteLine("6. Купить всю корзину");
                    Console.WriteLine("7. Мои заказы");
                    Console.WriteLine("8. Выйти из аккаунта");
                    Console.WriteLine("0. Выход");
                    Console.WriteLine();
                    int choice = ReadInt("Выберите пункт", 0, 8);
                    Console.WriteLine();

                    if (choice == 0)
                    {
                        return;
                    }
                    else if (choice == 1)
                    {
                        ShowProducts();
                    }
                    else if (choice == 2)
                    {
                        AddToCart();
                    }
                    else if (choice == 3)
                    {
                        ShowCart();
                    }
                    else if (choice == 4)
                    {
                        RemoveFromCart();
                    }
                    else if (choice == 5)
                    {
                        BuyOne();
                    }
                    else if (choice == 6)
                    {
                        BuyAll();
                    }
                    else if (choice == 7)
                    {
                        ShowOrders();
                    }
                    else if (choice == 8)
                    {
                        currentUser = null;
                        Console.WriteLine("Вы вышли из аккаунта");
                    }
                }
                Pause();
            }
        }
    }
}
