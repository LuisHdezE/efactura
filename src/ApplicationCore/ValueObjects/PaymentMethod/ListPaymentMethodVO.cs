using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.ValueObjects.PaymentMethod
{
    public class ListPaymentMethodVO
    {
        public long Id { get; private set; }
        public string Name { get; private set; }
    }
}
