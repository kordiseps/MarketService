using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace WebService.Asmx
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    public class MarketService : System.Web.Services.WebService
    {
        [WebMethod]
        public bool AddUser(string userName, string password)
        {
            try
            {
                MarketEntities dbContext = new MarketEntities();
                var newUser = new Users();
                newUser.Name = userName;
                newUser.Password = password;
                dbContext.Users.Add(newUser);
                dbContext.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("dbContext.SaveChanges Error : " + ex.Message);
            }
            return false;
        }

        [WebMethod]
        public string Login(string userName, string password)
        {

            MarketEntities dbContext = new MarketEntities();
            var userAvailable = false;
            try
            {
                userAvailable = dbContext.Users.Any(user => user.Name == userName && user.Password == password);
            }
            catch { }
            if (userAvailable)
            {
                var UserId = dbContext.Users.First(user => user.Name == userName && user.Password == password).Id;

                if (Authenticated(UserId))
                {
                    return dbContext.Sessions.First(session => session.UserId == UserId).Id.ToString();
                }
                else
                {
                    Sessions newSession = new Sessions();
                    newSession.Id = new Guid().ToString();
                    newSession.UserId = UserId;
                    newSession.CreateTime = DateTime.Now;
                    newSession.ExpireTime = DateTime.Now.AddMinutes(5);
                    try
                    {
                        dbContext.Sessions.Add(newSession);
                        dbContext.SaveChanges();
                        return newSession.Id.ToString();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("dbContext.SaveChanges Error : " + ex.Message);
                    }
                }
            }


            return string.Empty;
        }

        [WebMethod]
        public void CreateDummyProduct()
        {
            MarketEntities dbContext = new MarketEntities();
            ProductHeaders productHeader = new ProductHeaders();
            productHeader.Name = Dummy.ProductName();
            productHeader.Description = Dummy.RandomString(25);
            dbContext.ProductHeaders.Add(productHeader);
            dbContext.SaveChanges();
            var detailCount = Dummy.Next(5);
            for (int i = 0; i < detailCount; i++)
            {
                Products product = new Products();
                product.ProductHeaderId = productHeader.Id;
                product.Color = Dummy.Color();
                product.Size = Dummy.Size().ToString();
                product.UnitPrice = (decimal)Dummy.Price(productHeader.Name.Length);
                dbContext.Products.Add(product);
                dbContext.SaveChanges();
            }
        }

        [WebMethod]
        public ProductModel GetProductById(int id)
        {
            MarketEntities dbContext = new MarketEntities();
            var product = dbContext.Products.FirstOrDefault(p => p.Id == id);
            var productHeader = dbContext.ProductHeaders.FirstOrDefault(p => p.Id == product.ProductHeaderId);
            ProductModel productModel = new ProductModel();
            productModel.Name = productHeader.Name;
            productModel.Description = productHeader.Description;
            productModel.Color = product.Color;
            productModel.Size = product.Size;
            productModel.Price = product.UnitPrice;
            return productModel;
        }

        [WebMethod]
        public List<string> GetProductList()
        {
            MarketEntities dbContext = new MarketEntities();
            var Result = new List<string>();
            var ProductListOnDb = dbContext.Products.ToList();
            foreach (var product in ProductListOnDb)
            {
                var headers = dbContext.ProductHeaders.Where(header => header.Id == product.ProductHeaderId);
                foreach (var header in headers)
                {
                    Result.Add($"{product.Id} : {header.Name} - {product.Color} - {product.Size} - {product.UnitPrice} ({header.Description}) ");
                }
            }
            return Result;
        }

        [WebMethod]
        public bool AddToCart(string sessionId, int productId, int productCount)
        {

            try
            {
                MarketEntities dbContext = new MarketEntities();
                var UserId = GetUserId(sessionId);
                Carts cart = new Carts();
                cart.UserId = UserId;
                cart.ProductId = productId;
                cart.ProductCount = Convert.ToInt16(productCount);
                dbContext.Carts.Add(cart);
                dbContext.SaveChanges();
                return true;
            }
            catch (Exception aa)
            {
                Console.WriteLine(aa.Message);
            }
            return false;
        }

        [WebMethod]
        public List<string> GetCardContent(string sessionId)
        {
            var Result = new List<string>();
            MarketEntities dbContext = new MarketEntities();
            var UserId = GetUserId(sessionId);
            var cartContents = dbContext.Carts.Where(c => c.UserId == UserId).ToList();
            foreach (var cartContent in cartContents)
            {
                var product = dbContext.Products.First(p => p.Id == cartContent.ProductId);
                var header = dbContext.ProductHeaders.First(h => h.Id == product.ProductHeaderId);
                Result.Add($"{product.Id} : {header.Name} - {product.Color} - {product.Size} - {product.UnitPrice} x {cartContent.ProductCount} adet ({header.Description}) ");
            }
            return Result;
        }

        [WebMethod]
        public bool UpdateProductCountFromCart(string sessionId, int productId, int productCount)
        {
            try
            {
                MarketEntities dbContext = new MarketEntities();
                var UserId = GetUserId(sessionId);
                var cartContents = dbContext.Carts.First(c => c.UserId == UserId && c.ProductId == productId);
                cartContents.ProductCount = Convert.ToInt16(productCount);
                dbContext.SaveChanges();
                return true;
            }
            catch (Exception aa)
            {
                Console.WriteLine(aa.Message);
            }

            return false;
        }

        [WebMethod]
        public bool RemoveProductFromCart(string sessionId, int productId)
        {
            try
            {
                MarketEntities dbContext = new MarketEntities();
                var UserId = GetUserId(sessionId);
                var productToRemove = dbContext.Carts.First(c => c.UserId == UserId && c.ProductId == productId);
                dbContext.Carts.Remove(productToRemove);
                dbContext.SaveChanges();
                return true;
            }
            catch (Exception aa)
            {
                Console.WriteLine(aa.Message);
            }

            return false;

        }



        //give the order 
        //pay the amount
        //check the order



        private bool Authenticated(string sessionId)
        {
            MarketEntities dbContext = new MarketEntities();
            var CurrentSession = dbContext.Sessions.Where(session => session.Id == sessionId && session.ExpireTime > DateTime.Now).ToList();
            if (CurrentSession != null && CurrentSession.Count > 0)
            {
                try
                {
                    PostponeExpireTime(CurrentSession.First().Id);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("dbContext.Sessions.Any Error : " + ex.Message);
                }

            }
            return false;

        }
        private bool Authenticated(int userId)
        {
            MarketEntities dbContext = new MarketEntities();
            var CurrentSession = dbContext.Sessions.Where(session => session.UserId == userId && session.ExpireTime > DateTime.Now).ToList();
            if (CurrentSession != null && CurrentSession.Count > 0)
            {
                try
                {
                    PostponeExpireTime(CurrentSession.First().Id);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("dbContext.Sessions.Any Error : " + ex.Message);
                }

            }
            return false;
        }
        private void PostponeExpireTime(string sessionId)
        {
            MarketEntities dbContext = new MarketEntities();
            dbContext.Sessions.First(session => session.Id == sessionId).ExpireTime = DateTime.Now.AddMinutes(2);
            dbContext.SaveChanges();
        }
        private int GetUserId(string sessionId)
        {
            MarketEntities dbContext = new MarketEntities();
            return dbContext.Sessions.First(session => session.Id == sessionId).UserId;
        }
        
    }
}
