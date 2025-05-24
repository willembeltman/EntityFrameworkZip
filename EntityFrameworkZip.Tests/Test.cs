
//using System;
//using System.IO;
//using System.Linq;

//public static class CompanyEntityFactory
//{
//    public static void EntityWrite(BinaryWriter writer, EntityFrameworkZip.Tests.Company value, EntityFrameworkZip.DbContext objDb)
//    {
//        var db = objDb as EntityFrameworkZip.Tests.MyDbContext;

//        writer.Write(value.Id);
//        if (value.OwnerId == null)
//            writer.Write(true);
//        else
//        {
//            writer.Write(false);
//            writer.Write(value.OwnerId.Value);
//        }
//        if (value.Name == null)
//            writer.Write(true);
//        else
//        {
//            writer.Write(false);
//            writer.Write(value.Name);
//        }

//        if (value.Finance == null)
//            writer.Write(true);
//        else
//        {
//            writer.Write(false);
//            var FinanceEntityFactory = EntityFrameworkZip.GeneratedCode.EntityFactoryCollection.GetOrCreate<EntityFrameworkZip.Tests.CompanyFinance>(db);
//            FinanceEntityFactory.Write(writer, value.Finance, db);
//        }
//    }
//    public static EntityFrameworkZip.Tests.Company EntityRead(BinaryReader reader, EntityFrameworkZip.DbContext objDb)
//    {
//        var db = objDb as EntityFrameworkZip.Tests.MyDbContext;
//        if (db == null) throw new Exception("dbContext is not of type EntityFrameworkZip.Tests.MyDbContext");

//        var Id = reader.ReadInt64();
//        System.Int64? OwnerId = null;
//        if (!reader.ReadBoolean())
//        {
//            OwnerId = reader.ReadInt64();
//        }
//        System.String? Name = null;
//        if (!reader.ReadBoolean())
//        {
//            Name = reader.ReadString();
//        }

//        EntityFrameworkZip.Tests.CompanyFinance Finance = null;
//        if (!reader.ReadBoolean())
//        {
//            var FinanceEntityFactory = EntityFrameworkZip.GeneratedCode.EntityFactoryCollection.GetOrCreate<EntityFrameworkZip.Tests.CompanyFinance>(db);
//            Finance = FinanceEntityFactory.Read(reader, db);
//        }

//        var item = new EntityFrameworkZip.Tests.Company
//        {
//            Id = Id,
//            OwnerId = OwnerId,
//            Name = Name,
//            Finance = Finance,
//        };

//        return item;
//    }

//    public static void EntityExtend(EntityFrameworkZip.Tests.Company item, EntityFrameworkZip.DbContext objDb)
//    {
//        var db = objDb as EntityFrameworkZip.Tests.MyDbContext;
//        if (db == null) throw new Exception("dbContext is not of type EntityFrameworkZip.Tests.MyDbContext");

//        if (item.Employees != null &&
//            item.Employees is not EntityFrameworkZip.Navigation.LazyEntityCollectionNull<EntityFrameworkZip.Tests.Person, EntityFrameworkZip.Tests.Company>)
//        {
//            foreach (var subitem in item.Employees)
//            {
//                if (subitem.CompanyId != item.Id)
//                    subitem.CompanyId = item.Id;
//                db.People.Attach(subitem);
//            }
//        }
//        if (item.Employees == null ||
//            item.Employees.GetType() != typeof(EntityFrameworkZip.Navigation.LazyEntityCollectionNull<EntityFrameworkZip.Tests.Person, EntityFrameworkZip.Tests.Company>))
//        {
//            item.Employees = new EntityFrameworkZip.Navigation.LazyEntityCollectionNull<EntityFrameworkZip.Tests.Person, EntityFrameworkZip.Tests.Company>(
//                db.People,
//                item,
//                (foreign) => foreign.CompanyId,
//                (foreign, value) => { foreign.CompanyId = value; });
//        }
//        if (item.OwnerPerson != null &&
//            item.OwnerPerson.GetType() != typeof(EntityFrameworkZip.Navigation.LazyEntityNull<EntityFrameworkZip.Tests.Person, EntityFrameworkZip.Tests.Company>) &&
//            item.OwnerPerson.Value != null)
//        {
//            var subitem = item.OwnerPerson.Value;
//            if (item.OwnerId != subitem.Id)
//                item.OwnerId = subitem.Id;
//            db.People.Attach(subitem);
//        }

//        if (item.OwnerPerson == null ||
//            item.OwnerPerson.GetType() != typeof(EntityFrameworkZip.Navigation.LazyEntityNull<EntityFrameworkZip.Tests.Person, EntityFrameworkZip.Tests.Company>))
//        {
//            item.OwnerPerson = new EntityFrameworkZip.Navigation.LazyEntityNull<EntityFrameworkZip.Tests.Person, EntityFrameworkZip.Tests.Company>(
//                db.People,
//                item,
//                (foreign) => foreign.OwnerId,
//                (foreign, value) => { foreign.OwnerId = value; });
//        }
//        if (item.Finance != null)
//        {
//            var FinanceEntityFactory = EntityFrameworkZip.GeneratedCode.EntityFactoryCollection.GetOrCreate<EntityFrameworkZip.Tests.CompanyFinance>(db);
//            FinanceEntityFactory.SetNavigationProperties(item.Finance, db);
//        }
//    }
//    public static bool EntityFindForeignKeyUsage(EntityFrameworkZip.Tests.Company item, EntityFrameworkZip.DbContext objDb, bool removeIfFound)
//    {
//        var db = objDb as EntityFrameworkZip.Tests.MyDbContext;
//        if (db == null) throw new Exception("dbContext is not of type EntityFrameworkZip.Tests.MyDbContext");
//        var res = false;
//        if (removeIfFound)
//        {
//            var listPeople1 = db.People.Where(a => a.CompanyId == item.Id);
//            foreach (var item1 in listPeople1)
//            {
//                res = true;
//                item1.CompanyId = null;
//            }
//        }
//        else
//        {
//            if (db.People.Any(a => a.CompanyId == item.Id))
//                throw new Exception("Cannot delete EntityFrameworkZip.Tests.Company, id #" + item.Id + ", from Company. EntityFrameworkZip.Tests.MyDbContext.People.CompanyId has a reference towards it. Please remove the reference.");
//        }
//        return res;
//    }
//}
//public static class PersonEntityFactory
//{
//    public static void EntityWrite(BinaryWriter writer, EntityFrameworkZip.Tests.Person value, EntityFrameworkZip.DbContext objDb)
//    {
//        var db = objDb as EntityFrameworkZip.Tests.MyDbContext;

//        writer.Write(value.Id);
//        if (value.CompanyId == null)
//            writer.Write(true);
//        else
//        {
//            writer.Write(false);
//            writer.Write(value.CompanyId.Value);
//        }
//        if (value.Name == null)
//            writer.Write(true);
//        else
//        {
//            writer.Write(false);
//            writer.Write(value.Name);
//        }
//        if (value.Rank == null)
//            writer.Write(true);
//        else
//        {
//            writer.Write(false);
//            writer.Write((int)value.Rank.Value);
//        }
//        writer.Write(value.CreatedDate.ToBinary()); ;
//        if (value.LastModifiedDate == null)
//            writer.Write(true);
//        else
//        {
//            writer.Write(false);
//            writer.Write(value.LastModifiedDate.Value.ToBinary()); ;
//        }
//    }
//    public static EntityFrameworkZip.Tests.Person EntityRead(BinaryReader reader, EntityFrameworkZip.DbContext objDb)
//    {
//        var db = objDb as EntityFrameworkZip.Tests.MyDbContext;
//        if (db == null) throw new Exception("dbContext is not of type EntityFrameworkZip.Tests.MyDbContext");

//        var Id = reader.ReadInt64();
//        System.Int64? CompanyId = null;
//        if (!reader.ReadBoolean())
//        {
//            CompanyId = reader.ReadInt64();
//        }
//        System.String? Name = null;
//        if (!reader.ReadBoolean())
//        {
//            Name = reader.ReadString();
//        }
//        EntityFrameworkZip.Tests.RankEnum? Rank = null;
//        if (!reader.ReadBoolean())
//        {
//            Rank = (EntityFrameworkZip.Tests.RankEnum)reader.ReadInt32();
//        }
//        var CreatedDate = DateTime.FromBinary(reader.ReadInt64());
//        System.DateTime? LastModifiedDate = null;
//        if (!reader.ReadBoolean())
//        {
//            LastModifiedDate = DateTime.FromBinary(reader.ReadInt64());
//        }

//        var item = new EntityFrameworkZip.Tests.Person
//        {
//            Id = Id,
//            CompanyId = CompanyId,
//            Name = Name,
//            Rank = Rank,
//            CreatedDate = CreatedDate,
//            LastModifiedDate = LastModifiedDate,
//        };

//        return item;
//    }

//    public static void EntityExtend(EntityFrameworkZip.Tests.Person item, EntityFrameworkZip.DbContext objDb)
//    {
//        var db = objDb as EntityFrameworkZip.Tests.MyDbContext;
//        if (db == null) throw new Exception("dbContext is not of type EntityFrameworkZip.Tests.MyDbContext");

//        if (item.Company != null &&
//            item.Company.GetType() != typeof(EntityFrameworkZip.Navigation.LazyEntityNull<EntityFrameworkZip.Tests.Company, EntityFrameworkZip.Tests.Person>) &&
//            item.Company.Value != null)
//        {
//            var subitem = item.Company.Value;
//            if (item.CompanyId != subitem.Id)
//                item.CompanyId = subitem.Id;
//            db.Companies.Attach(subitem);
//        }

//        if (item.Company == null ||
//            item.Company.GetType() != typeof(EntityFrameworkZip.Navigation.LazyEntityNull<EntityFrameworkZip.Tests.Company, EntityFrameworkZip.Tests.Person>))
//        {
//            item.Company = new EntityFrameworkZip.Navigation.LazyEntityNull<EntityFrameworkZip.Tests.Company, EntityFrameworkZip.Tests.Person>(
//                db.Companies,
//                item,
//                (foreign) => foreign.CompanyId,
//                (foreign, value) => { foreign.CompanyId = value; });
//        }
//    }
//    public static bool EntityFindForeignKeyUsage(EntityFrameworkZip.Tests.Person item, EntityFrameworkZip.DbContext objDb, bool removeIfFound)
//    {
//        var db = objDb as EntityFrameworkZip.Tests.MyDbContext;
//        if (db == null) throw new Exception("dbContext is not of type EntityFrameworkZip.Tests.MyDbContext");
//        var res = false;
//        if (removeIfFound)
//        {
//            var listCompanies1 = db.Companies.Where(a => a.OwnerId == item.Id);
//            foreach (var item1 in listCompanies1)
//            {
//                res = true;
//                item1.OwnerId = null;
//            }
//        }
//        else
//        {
//            if (db.Companies.Any(a => a.OwnerId == item.Id))
//                throw new Exception("Cannot delete EntityFrameworkZip.Tests.Person, id #" + item.Id + ", from Person. EntityFrameworkZip.Tests.MyDbContext.Companies.OwnerId has a reference towards it. Please remove the reference.");
//        }
//        return res;
//    }
//}