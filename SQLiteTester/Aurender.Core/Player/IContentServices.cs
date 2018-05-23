using System;
using System.Collections.Generic;

namespace Aurender.Core.Contents
{
    public interface IContentService
    {
        Boolean HasComposers();
        Boolean HasConductors();
        Boolean HasFolder();

        object ServiceLogo { get; }
        object ServiceLogoForSelected { get; }

        String Name { get; }

        List<String> GetNavigationTypeTitles();

        /// <summary>
        /// For local it will be list of folder filters
        /// For other, it will be Feature/Section titles (Weekly chart, Monthly chart etc.)
        /// </summary>
        /// <returns>The section filter titles.</returns>
        /// <param name="navigationType">Navigation type. Something like, Songs, Playlists, Composers</param>
        List<String> GetSectionFilterTitles(String navigationType);
        String LastSelectedSectionTitle(String navigationType);

        Object GetDataManager(String title);
        Object GetFilteredDataManager(String sectionTitle);

        Dictionary<String, Boolean> SupportedFeatures { get; }
    }
/*
	public interface IStreammingServiceEvent
	{
		event Action<IStreamingService, Boolean> OnLoggedIn;
		event Action<IStreamingService, Boolean> AfterLoggedOut;

		event Action<Object, Dictionary<String, String>> GotMeesage;
	}

	public interface IStreamingService : IContentService, IStreammingServiceEvent
    {
        String ServiceSiteURL { get; }
        String ServiceJoinURL { get; }

        List<String> SupportedQuality { get; }
        List<String> AvailableQuality { get; }

        ImageSource GetLogoWithTitle();
        ImageSource GetServiceLogoForAlbum();

        ContentType ServiceType {get;}
    }
    */

    interface IWindowedData
    {
    }

    public interface IDataManagerBase<T> : IEnumerable<T>
    {
    }

    public interface AbstractManager : IDataManagerBase<IDatabaseItem>
    {
    }


/*
	public interface IContentServices : IStreammingServiceEvent
    {
        List<IContentService> Services { get; }

        IContentService CurrentService { get; }

        IContentService SwitchToService(String serviceName);

    }*/
}
