using FluentValidation;
using System;

namespace Nop.Web.Framework.Events
{
    public class ValidatorPreparedEvent
    {
        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="modelType">Type of model to be validated</param>
        /// <param name="validator">Validator</param>
        public ValidatorPreparedEvent(Type modelType, IValidator validator)
        {
            this.ModelType = modelType;
            this.Validator = validator;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a model
        /// </summary>
        public Type ModelType { get; private set; }
        public IValidator Validator { get; private set; }

        #endregion
    }
}
