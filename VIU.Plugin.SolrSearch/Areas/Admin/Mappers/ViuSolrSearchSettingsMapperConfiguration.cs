using AutoMapper;
using Nop.Core.Infrastructure.Mapper;
using VIU.Plugin.SolrSearch.Areas.Admin.Models;
using VIU.Plugin.SolrSearch.Settings;

namespace VIU.Plugin.SolrSearch.Areas.Admin.Mappers
{
    public class ViuSolrSearchSettingsMapperConfiguration : Profile, IOrderedMapperProfile
    {
        public ViuSolrSearchSettingsMapperConfiguration()
        {
            CreateMap<ViuSolrSearchSettings, ViuSolrSearchSettingsModel>()
                .ForMember(model => model.SelectedFilterableSpecificationAttributeIds, options => options.Ignore()).ReverseMap();
        }

        public int Order => 1;
    }
}