using System.Collections.Generic;
using Library.API.Services;

namespace Library.API.Models
{
    public class LinkedCollectionResourceWrapperDto<T> : LinkedResourceBaseDto
        where T : LinkedResourceBaseDto
    {
        public IEnumerable<T> Value { get; set; }

        public LinkedCollectionResourceWrapperDto(IEnumerable<T> value )
        {
            Value = value;
        }
    }
}