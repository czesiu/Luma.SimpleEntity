﻿using System.ComponentModel.DataAnnotations;

namespace ServerClassLib
{
    public partial class TestEntity2
    {
        [Key]
        public string TheKey
        {
            get
            {
                return null;
            }
        }

        public int TheValue
        {
            get
            {
                return 0;
            }
            set
            {
                // nothing
            }
        }
    }
}
