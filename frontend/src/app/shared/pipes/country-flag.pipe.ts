import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'countryFlag', standalone: true })
export class CountryFlagPipe implements PipeTransform {
  transform(countryName: string | null | undefined): string {
    if (!countryName) return '';
    const code = COUNTRY_CODES[countryName.toLowerCase()];
    if (!code) return '🏳️';
    return countryCodeToEmoji(code);
  }
}

function countryCodeToEmoji(code: string): string {
  return [...code.toUpperCase()]
    .map(c => String.fromCodePoint(0x1f1e6 - 65 + c.charCodeAt(0)))
    .join('');
}

const COUNTRY_CODES: Record<string, string> = {
  'cameroon': 'CM', 'cameroun': 'CM',
  'nigeria': 'NG', 'ghana': 'GH', 'kenya': 'KE',
  'south africa': 'ZA', 'afrique du sud': 'ZA',
  'ethiopia': 'ET', 'éthiopie': 'ET',
  'tanzania': 'TZ', 'tanzanie': 'TZ',
  'uganda': 'UG', 'ouganda': 'UG',
  'rwanda': 'RW',
  'senegal': 'SN', 'sénégal': 'SN',
  "côte d'ivoire": 'CI', "cote d'ivoire": 'CI', 'ivory coast': 'CI',
  'mali': 'ML', 'burkina faso': 'BF', 'niger': 'NE', 'chad': 'TD', 'tchad': 'TD',
  'gabon': 'GA', 'congo': 'CG', 'democratic republic of congo': 'CD',
  'republic of congo': 'CG', 'république du congo': 'CG',
  'angola': 'AO', 'mozambique': 'MZ', 'zambia': 'ZM', 'zimbabwe': 'ZW',
  'botswana': 'BW', 'namibia': 'NA', 'namibie': 'NA',
  'madagascar': 'MG', 'mauritius': 'MU', 'île maurice': 'MU',
  'morocco': 'MA', 'maroc': 'MA', 'algeria': 'DZ', 'algérie': 'DZ',
  'tunisia': 'TN', 'tunisie': 'TN', 'egypt': 'EG', 'égypte': 'EG',
  'libya': 'LY', 'libye': 'LY',
  'france': 'FR', 'belgium': 'BE', 'belgique': 'BE',
  'switzerland': 'CH', 'suisse': 'CH',
  'germany': 'DE', 'allemagne': 'DE',
  'united kingdom': 'GB', 'royaume-uni': 'GB',
  'spain': 'ES', 'espagne': 'ES',
  'italy': 'IT', 'italie': 'IT',
  'portugal': 'PT',
  'united states': 'US', 'états-unis': 'US', 'usa': 'US',
  'canada': 'CA', 'brazil': 'BR', 'brésil': 'BR',
  'china': 'CN', 'chine': 'CN',
  'india': 'IN', 'inde': 'IN',
  'japan': 'JP', 'japon': 'JP',
  'australia': 'AU', 'australie': 'AU',
  'other': 'UN', 'autre': 'UN',
};
