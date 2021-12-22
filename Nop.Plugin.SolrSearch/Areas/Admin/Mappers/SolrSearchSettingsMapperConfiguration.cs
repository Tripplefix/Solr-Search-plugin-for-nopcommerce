using AutoMapper;
using Nop.Core.Infrastructure.Mapper;
using Nop.Plugin.SolrSearch.Areas.Admin.Models;
using Nop.Plugin.SolrSearch.Settings;

namespace Nop.Plugin.SolrSearch.Areas.Admin.Mappers
{
    public class SolrSearchSettingsMapperConfiguration : Profile, IOrderedMapperProfile
    {
        public SolrSearchSettingsMapperConfiguration()
        {
            CreateMap<SolrSearchSettings, SolrSearchSettingsModel>()
                .ForMember(model => model.SelectedFilterableSpecificationAttributeIds, options => options.Ignore()).ReverseMap();
        }

        public int Order => 1;
    }
}