using System.ComponentModel.DataAnnotations;

namespace ServerClassLib
{
    public class TestValidator
    {
        // Tests shared ctor
        public TestValidator(string notUsed)
        {
        }

        public static ValidationResult IsValid(TestEntity testEntity, ValidationContext validationContext)
        {
            return ValidationResult.Success;
        }
    }
}
