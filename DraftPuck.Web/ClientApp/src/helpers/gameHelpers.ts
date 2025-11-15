import PeriodType from "@/enums/periodType"

export function getOrdinal(periodNumber: number, periodType: PeriodType) {
  if (periodType === PeriodType.Regulation) {
    if (periodNumber === 1) return '1st'
    if (periodNumber === 2) return '2nd'
    if (periodNumber === 3) return '3rd'
  } else if (periodType === PeriodType.Overtime) {
    return periodNumber > 4 ? `${periodNumber - 3}OT` : 'OT'
  } else if (periodType === PeriodType.Shootout) {
    return 'SO'
  }
}
