using Microsoft.EntityFrameworkCore;
using System;

namespace Automatica.Core.EF.Extensions
{
    public static class ContextExtensions
    {
        public static void AddOrUpdate<T>(this DbContext ctx, T entity, Func<T, object> keyValue) where T : class
        {
            var obj = ctx.Find<T>(keyValue(entity));

            if (obj != null)
            {
                ctx.Entry(entity).CurrentValues.SetValues(entity);
            }
            else
            {
                ctx.Add(entity);
            }


        }
    }
}
