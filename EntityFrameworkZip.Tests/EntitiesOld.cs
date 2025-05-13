//using EntityFrameworkZip.Attributes;
//using EntityFrameworkZip.Interfaces;

//// Disable nullable warnings for this file, as the behavior of will make the properties behave like they are not nullable.
//#nullable disable

//// A simple Person entity implementing IEntity.
//// Includes a reference to a Company via a Lazy<Company> property.
//public class Person : IEntity
//{
//    public long Id { get; set; }
//    public long CompanyId { get; set; }
//    public string Name { get; set; }

//    // Lazily loaded reference to the related Company.
//    public virtual Lazy<Company> Company { get; set; } = new Lazy<Company>(() => null);
//}

//// Disable nullable warnings for this file, as the behavior of will make the properties behave like they are not nullable.
//#nullable disable

//// A simple Company entity implementing IEntity.
//// Includes a collection of Employees and a Lazy-loaded Owner reference.
//public class Company : IEntity
//{
//    public long Id { get; set; }
//    public long OwnerId { get; set; }
//    public string Name { get; set; }

//    // A lazy loaded collection of Employees, which will be set when the entity is assoiciated with the context.
//    // Normally this property would be null till it is assoicated with the context with Add, or when
//    // retreiving the entity from the context, but in this example we initialize it with a new list.
//    // This will allow us to add employees to the collection before adding the Company to the context.
//    // After adding it to the context the stored Person's in the list will also be added to the context.
//    public virtual ICollection<Person> Employees { get; set; } = new List<Person>();

//    // The ForeignKey attribute specifies a different foreign key name for the OwnerPerson property.
//    // Default it will take the name of the property (OwnerPerson) and append Id
//    // to it (OwnerPersonId), which is not the name of the foreign key in this example, so we
//    // specify it explicitly, so the engine will just use OwnerId instead.
//    [ForeignKey("OwnerId")]
//    // Lazily loaded reference to the Owner (a Person). Will be accesseble after adding to the
//    // context.
//    public virtual Lazy<Person> OwnerPerson { get; set; }

//    // The NotMapped attribute specifies that this property should not be mapped to the database.
//    // This is manditory for the engine as it will otherwise try to serialize the list, which 
//    // is not supported. If you need to store a list, use the EntityFrameworkZip orm to make
//    // another dataset.
//    // A list of temporary todo items. This is just a simple list of strings, but it could be
//    // any type, as long as it has the NotMapped attribute.
//    [NotMapped]
//    public List<string> TemporaryTodoItems { get; set; } = new List<string>();

//    // A reference to a sub entity. This class is held against the same principles as a entity, so
//    // it supports:
//    // - Primative types.
//    // - DateTime's.
//    // - ExtendedProperties with 'virtual Lazy<>' (with optional ForeignKeyAttribute).
//    // - ExtendedLists with 'virtual ICollection<>' or 'virtual IEnumerable<>' (with optional ForeignKeyAttribute).
//    // - And non-generic struct's and classes(which in turn support the same kind of properties).
//    public CompanyFinance Finance { get; set; } = new CompanyFinance();
//}

//public class CompanyFinance
//{
//    public decimal Revenue { get; set; }
//    public decimal Expenses { get; set; }
//    // Properties with only getters will automatically be skipped, so no need for a NotMapped
//    // attribute.
//    public decimal Profit => Revenue - Expenses;

//    // A foreign key inside of the extended CompanyFinance class.
//    public long HeadOfFinancePersonId { get; set; }

//    // Specify the custom foreign key name for the HeadOfFinancePerson property. This need to
//    // be inside this class to work, you cannot specify a foreign key in the parent class as the
//    // engine has no knowedge of that class in relationship to this class.
//    [ForeignKey("HeadOfFinancePersonId")]
//    // Lazily loaded reference to the HeadOfFinancePerson (a Person). Will be accesseble after
//    // adding to the context.
//    public virtual Lazy<Person> HeadOfFinancePerson { get; set; }
//}
